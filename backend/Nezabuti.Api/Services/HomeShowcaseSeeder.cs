using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Nezabuti.Api.Models;
using Nezabuti.Api.Repositories;

namespace Nezabuti.Api.Services;

public interface IHomeShowcaseSeeder
{
    Task EnsureSeededAsync(CancellationToken ct = default);
}

/// <summary>
/// Development-only import of packaged How-it-works slides into site_settings.
/// Never runs in Production. Never overwrites an already configured showcase,
/// except a local refresh of seed mobile images when the packaged revision changes.
/// Images go through the same home-upload pipeline as the admin UI.
/// </summary>
public sealed class HomeShowcaseSeeder : IHomeShowcaseSeeder
{
    public const string SeedFolderName = "home-showcase";
    public const string MobileRevision = "clean-v2";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISiteSettingsRepository _settings;
    private readonly IPhotoService _photos;
    private readonly IHostEnvironment _env;
    private readonly ILogger<HomeShowcaseSeeder> _logger;
    private readonly string _seedRoot;

    public HomeShowcaseSeeder(
        ISiteSettingsRepository settings,
        IPhotoService photos,
        IHostEnvironment env,
        ILogger<HomeShowcaseSeeder> logger)
        : this(settings, photos, env, logger, Path.Combine(AppContext.BaseDirectory, "Seed", SeedFolderName))
    {
    }

    public HomeShowcaseSeeder(
        ISiteSettingsRepository settings,
        IPhotoService photos,
        IHostEnvironment env,
        ILogger<HomeShowcaseSeeder> logger,
        string seedRoot)
    {
        _settings = settings;
        _photos = photos;
        _env = env;
        _logger = logger;
        _seedRoot = seedRoot;
    }

    public async Task EnsureSeededAsync(CancellationToken ct = default)
    {
        if (!_env.IsDevelopment())
        {
            _logger.LogInformation("Home showcase seed skipped: environment is {Environment}", _env.EnvironmentName);
            return;
        }

        var current = await _settings.GetAsync(ct);
        if (HasConfiguredShowcase(current.HomeShowcase))
        {
            if (ShouldRefreshSeedMobile(current.HomeShowcase))
            {
                await RefreshSeedMobileImagesAsync(current, ct);
                return;
            }

            _logger.LogInformation("Home showcase seed skipped: settings already exist");
            return;
        }

        var jsonPath = Path.Combine(_seedRoot, "home-showcase.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogInformation("Home showcase seed skipped: {Path} not found", jsonPath);
            return;
        }

        var file = JsonSerializer.Deserialize<HomeShowcaseSeedFile>(await File.ReadAllTextAsync(jsonPath, ct), JsonOptions);
        if (file?.Slides is null || file.Slides.Count == 0)
        {
            _logger.LogWarning("Home showcase seed file is empty: {Path}", jsonPath);
            return;
        }

        var photos = new Dictionary<string, PhotoRef>(StringComparer.OrdinalIgnoreCase);
        foreach (var slide in file.Slides)
        {
            await ImportNamedAsync(photos, "desktop", slide.DesktopImageName ?? slide.ImageName, ct);
            await ImportNamedAsync(photos, "mobile", slide.MobileImageName, ct);
        }

        await ImportNamedAsync(photos, "desktop", file.Demo?.PreviewDesktopImageName ?? file.Demo?.PreviewImageName, ct);
        await ImportNamedAsync(photos, "mobile", file.Demo?.PreviewMobileImageName, ct);

        if (file.Slides.Any(s => Lookup(photos, s.DesktopImageName ?? s.ImageName) is null))
        {
            _logger.LogInformation("Home showcase seed skipped: packaged images are not present");
            return;
        }

        var slides = file.Slides
            .OrderBy(s => s.SortOrder)
            .Select((s, index) =>
            {
                var desktop = Lookup(photos, s.DesktopImageName ?? s.ImageName);
                var mobile = Lookup(photos, s.MobileImageName);
                return new HowItWorksSlide
                {
                    Id = $"seed-how-{s.SortOrder:00}",
                    Title = s.Title ?? string.Empty,
                    Description = s.Description ?? string.Empty,
                    ShortCaption = s.ShortCaption,
                    Image = desktop,
                    DesktopImage = desktop,
                    MobileImage = mobile,
                    AltText = s.AltText,
                    SortOrder = index,
                    Enabled = s.Enabled
                };
            })
            .ToList();

        PhotoRef? demoPreview = Lookup(photos, file.Demo?.PreviewDesktopImageName ?? file.Demo?.PreviewImageName)
            ?? slides.ElementAtOrDefault(1)?.DesktopImage;

        current.HomeShowcase = HomeShowcaseMapper.Normalize(new HomeShowcaseSettings
        {
            HowItWorksEnabled = file.HowItWorksEnabled,
            HowItWorksSlides = slides,
            DemoEnabled = file.Demo?.Enabled ?? false,
            DemoTitle = file.Demo?.Title ?? string.Empty,
            DemoDescription = file.Demo?.Description ?? string.Empty,
            DemoUrl = file.Demo?.Url ?? string.Empty,
            DemoPreviewImage = demoPreview,
            SeedMobileRevision = MobileRevision
        });
        HomeShowcaseMapper.ValidateForSave(current.HomeShowcase);
        await _settings.UpsertAsync(current, ct);
        _logger.LogInformation("Home showcase seeded with {Count} slides (development only)", current.HomeShowcase.HowItWorksSlides.Count);
    }

    public static bool HasConfiguredShowcase(HomeShowcaseSettings? showcase)
    {
        if (showcase is null)
        {
            return false;
        }

        if (showcase.HowItWorksSlides is { Count: > 0 })
        {
            return true;
        }

        if (showcase.DemoEnabled || !string.IsNullOrWhiteSpace(showcase.DemoUrl) || showcase.DemoPreviewImage is not null)
        {
            return true;
        }

        return false;
    }

    public static bool IsLocalSeedShowcase(HomeShowcaseSettings? showcase)
    {
        var slides = showcase?.HowItWorksSlides;
        return slides is { Count: > 0 } &&
               slides.All(s => s.Id.StartsWith("seed-how-", StringComparison.Ordinal));
    }

    public static bool ShouldRefreshSeedMobile(HomeShowcaseSettings? showcase) =>
        IsLocalSeedShowcase(showcase) &&
        !string.Equals(showcase!.SeedMobileRevision, MobileRevision, StringComparison.Ordinal);

    private async Task RefreshSeedMobileImagesAsync(SiteSettings current, CancellationToken ct)
    {
        var jsonPath = Path.Combine(_seedRoot, "home-showcase.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogInformation("Home showcase mobile refresh skipped: {Path} not found", jsonPath);
            return;
        }

        var file = JsonSerializer.Deserialize<HomeShowcaseSeedFile>(await File.ReadAllTextAsync(jsonPath, ct), JsonOptions);
        if (file?.Slides is null || file.Slides.Count == 0)
        {
            _logger.LogWarning("Home showcase mobile refresh skipped: seed file is empty");
            return;
        }

        var previous = HomeShowcaseMapper.Normalize(current.HomeShowcase);
        var previousIds = HomeShowcaseMapper.CollectPhotoIds(previous);
        var byId = previous.HowItWorksSlides.ToDictionary(s => s.Id, StringComparer.Ordinal);
        var photos = new Dictionary<string, PhotoRef>(StringComparer.OrdinalIgnoreCase);
        foreach (var seedSlide in file.Slides)
        {
            var id = $"seed-how-{seedSlide.SortOrder:00}";
            if (!byId.ContainsKey(id))
            {
                continue;
            }

            await ImportNamedAsync(photos, "mobile", seedSlide.MobileImageName, ct);
        }

        if (photos.Count == 0)
        {
            _logger.LogInformation("Home showcase mobile refresh skipped: packaged images are not present");
            return;
        }

        foreach (var seedSlide in file.Slides)
        {
            var id = $"seed-how-{seedSlide.SortOrder:00}";
            if (!byId.TryGetValue(id, out var existing))
            {
                continue;
            }

            var mobile = Lookup(photos, seedSlide.MobileImageName);
            if (mobile is not null)
            {
                existing.MobileImage = mobile;
            }
        }

        previous.SeedMobileRevision = MobileRevision;
        current.HomeShowcase = HomeShowcaseMapper.Normalize(previous);
        HomeShowcaseMapper.ValidateForSave(current.HomeShowcase);
        await _settings.UpsertAsync(current, ct);

        var keep = new HashSet<string>(HomeShowcaseMapper.CollectPhotoIds(current.HomeShowcase), StringComparer.Ordinal);
        foreach (var photoId in previousIds)
        {
            if (!keep.Contains(photoId))
            {
                await _photos.DeleteHomePhotoFilesAsync(photoId, ct);
            }
        }

        _logger.LogInformation("Home showcase mobile images refreshed to {Revision} (development only)", MobileRevision);
    }

    private static PhotoRef? Lookup(IReadOnlyDictionary<string, PhotoRef> photos, string? name) =>
        !string.IsNullOrWhiteSpace(name) && photos.TryGetValue(name, out var photo) ? photo : null;

    private async Task ImportNamedAsync(Dictionary<string, PhotoRef> photos, string folder, string? fileName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileName) || photos.ContainsKey(fileName))
        {
            return;
        }

        var photo = await TryImportImageAsync(folder, fileName, ct);
        if (photo is not null)
        {
            photos[fileName] = photo;
        }
    }

    private async Task<PhotoRef?> TryImportImageAsync(string folder, string fileName, CancellationToken ct)
    {
        var path = Path.Combine(_seedRoot, folder, fileName);
        if (!File.Exists(path))
        {
            path = Path.Combine(_seedRoot, "images", fileName);
        }

        if (!File.Exists(path))
        {
            _logger.LogInformation("Home showcase seed image missing: {Folder}/{File}", folder, fileName);
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await _photos.ProcessHomeUploadAsync(stream, "image/png", ct);
    }
}

internal sealed class HomeShowcaseSeedFile
{
    public bool HowItWorksEnabled { get; set; } = true;
    public List<HomeShowcaseSeedSlide> Slides { get; set; } = [];
    public HomeShowcaseSeedDemo? Demo { get; set; }
}

internal sealed class HomeShowcaseSeedSlide
{
    public int SortOrder { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ShortCaption { get; set; }
    public string? ImageName { get; set; }
    public string? DesktopImageName { get; set; }
    public string? MobileImageName { get; set; }
    public string? AltText { get; set; }
}

internal sealed class HomeShowcaseSeedDemo
{
    public bool Enabled { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? PreviewImageName { get; set; }
    public string? PreviewDesktopImageName { get; set; }
    public string? PreviewMobileImageName { get; set; }
}
