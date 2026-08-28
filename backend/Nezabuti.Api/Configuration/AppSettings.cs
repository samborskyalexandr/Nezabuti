namespace Nezabuti.Api.Configuration;

public class MongoSettings
{
    public const string SectionName = "Mongo";
    public string ConnectionString { get; set; } = "mongodb://mongodb:27017";
    public string DatabaseName { get; set; } = "nezabuti";
}

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "nezabuti";
    public string Audience { get; set; } = "nezabuti-admin";
    public int ExpirationHours { get; set; } = 12;
}

public class AdminSettings
{
    public const string SectionName = "Admin";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AppPublicSettings
{
    public const string SectionName = "App";
    public string PublicBaseUrl { get; set; } = "http://localhost:8088";
    public string[] AllowedOrigins { get; set; } = ["http://localhost:8088"];
}

public class ImageSettings
{
    public const string SectionName = "Images";
    public long MaxUploadBytes { get; set; } = 25 * 1024 * 1024;
    public int ThumbMaxDimension { get; set; } = 320;
    public int PreviewMaxDimension { get; set; } = 800;
    public int FullMaxDimension { get; set; } = 2000;
    public int WebpQuality { get; set; } = 80;
    public string UploadsRoot { get; set; } = "/app/uploads";
}
