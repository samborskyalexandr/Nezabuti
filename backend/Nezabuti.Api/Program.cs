using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using Nezabuti.Api.Configuration;
using Nezabuti.Api.Repositories;
using Nezabuti.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection(MongoSettings.SectionName));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection(AdminSettings.SectionName));
builder.Services.Configure<AppPublicSettings>(builder.Configuration.GetSection(AppPublicSettings.SectionName));
builder.Services.Configure<ImageSettings>(builder.Configuration.GetSection(ImageSettings.SectionName));

// Also bind from flat env-friendly keys used in docker-compose
builder.Services.PostConfigure<MongoSettings>(opts =>
{
    opts.ConnectionString = builder.Configuration["MONGO_CONNECTION_STRING"] ?? opts.ConnectionString;
    opts.DatabaseName = builder.Configuration["MONGO_DATABASE"] ?? opts.DatabaseName;
});
builder.Services.PostConfigure<JwtSettings>(opts =>
{
    opts.Secret = builder.Configuration["JWT_SECRET"] ?? opts.Secret;
    opts.Issuer = builder.Configuration["JWT_ISSUER"] ?? opts.Issuer;
    opts.Audience = builder.Configuration["JWT_AUDIENCE"] ?? opts.Audience;
});
builder.Services.PostConfigure<AdminSettings>(opts =>
{
    opts.Username = builder.Configuration["ADMIN_USERNAME"] ?? opts.Username;
    opts.Password = builder.Configuration["ADMIN_PASSWORD"] ?? opts.Password;
});
builder.Services.PostConfigure<AppPublicSettings>(opts =>
{
    opts.PublicBaseUrl = builder.Configuration["PUBLIC_BASE_URL"] ?? opts.PublicBaseUrl;
    var origins = builder.Configuration["ALLOWED_ORIGINS"];
    if (!string.IsNullOrWhiteSpace(origins))
    {
        opts.AllowedOrigins = origins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
});
builder.Services.PostConfigure<ImageSettings>(opts =>
{
    if (long.TryParse(builder.Configuration["IMAGE_MAX_UPLOAD_BYTES"], out var maxBytes))
    {
        opts.MaxUploadBytes = maxBytes;
    }

    if (int.TryParse(builder.Configuration["IMAGE_THUMB_MAX_DIMENSION"], out var thumb))
    {
        opts.ThumbMaxDimension = thumb;
    }

    if (int.TryParse(builder.Configuration["IMAGE_PREVIEW_MAX_DIMENSION"], out var preview))
    {
        opts.PreviewMaxDimension = preview;
    }

    if (int.TryParse(builder.Configuration["IMAGE_FULL_MAX_DIMENSION"], out var full))
    {
        opts.FullMaxDimension = full;
    }

    if (int.TryParse(builder.Configuration["IMAGE_WEBP_QUALITY"], out var quality))
    {
        opts.WebpQuality = quality;
    }

    opts.UploadsRoot = builder.Configuration["UPLOADS_ROOT"] ?? opts.UploadsRoot;
});

var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT_SECRET is required");

if (jwtSecret.Length < 32)
{
    throw new InvalidOperationException("JWT_SECRET must be at least 32 characters");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"] ?? "nezabuti",
            ValidAudience = builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"] ?? "nezabuti-admin",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Transport limits must exceed the application Images:MaxUploadBytes so oversized
// files reach the controller and return a controlled Ukrainian error.
const long MultipartTransportLimitBytes = 30L * 1024 * 1024;

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = MultipartTransportLimitBytes;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MultipartTransportLimitBytes;
});

var allowedOrigins = (builder.Configuration["ALLOWED_ORIGINS"]
    ?? builder.Configuration["App:AllowedOrigins"]
    ?? "http://localhost:8088")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IMongoContext, MongoContext>();
builder.Services.AddSingleton<IPublicIdGenerator, PublicIdGenerator>();
builder.Services.AddSingleton<IRichTextSanitizer, RichTextSanitizer>();
builder.Services.AddScoped<IMemorialRepository, MemorialRepository>();
builder.Services.AddScoped<ISiteSettingsRepository, SiteSettingsRepository>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IPlanLimitService, PlanLimitService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IMemorialService, MemorialService>();
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();

builder.Services.AddHealthChecks()
    .AddCheck<Nezabuti.Api.Health.MongoHealthCheck>("mongodb");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var mongo = scope.ServiceProvider.GetRequiredService<IMongoContext>();
    await mongo.EnsureIndexesAsync();
    var planRepo = scope.ServiceProvider.GetRequiredService<IPlanRepository>();
    await planRepo.EnsureBootstrapAsync();
}

var uploadsRoot = builder.Configuration["UPLOADS_ROOT"]
    ?? builder.Configuration["Images:UploadsRoot"]
    ?? "/app/uploads";
Directory.CreateDirectory(uploadsRoot);
Directory.CreateDirectory(Path.Combine(uploadsRoot, "memorials"));

app.UseCors("AppCors");
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});

app.MapControllers();
app.MapHealthChecks("/api/health/ready");

app.Run();
