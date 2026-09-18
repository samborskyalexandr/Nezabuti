using MongoDB.Bson.Serialization.Attributes;

namespace Nezabuti.Api.Models;

/// <summary>
/// Home-page presentation settings stored on the site_settings singleton.
/// Missing document fields deserialize as empty defaults.
/// </summary>
public class HomeShowcaseSettings
{
    [BsonElement("howItWorksEnabled")]
    public bool HowItWorksEnabled { get; set; } = true;

    [BsonElement("howItWorksSlides")]
    public List<HowItWorksSlide> HowItWorksSlides { get; set; } = [];

    [BsonElement("demoEnabled")]
    public bool DemoEnabled { get; set; }

    [BsonElement("demoTitle")]
    public string DemoTitle { get; set; } = string.Empty;

    [BsonElement("demoDescription")]
    public string DemoDescription { get; set; } = string.Empty;

    [BsonElement("demoUrl")]
    public string DemoUrl { get; set; } = string.Empty;

    [BsonElement("demoPreviewImage")]
    public PhotoRef? DemoPreviewImage { get; set; }

    /// <summary>
    /// Development seed marker so mobile assets can be refreshed without touching desktop/copy.
    /// Not exposed in admin DTOs.
    /// </summary>
    [BsonElement("seedMobileRevision")]
    public string? SeedMobileRevision { get; set; }
}

public class HowItWorksSlide
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("shortCaption")]
    public string? ShortCaption { get; set; }

    /// <summary>Legacy desktop image. Prefer <see cref="DesktopImage"/>.</summary>
    [BsonElement("image")]
    public PhotoRef? Image { get; set; }

    [BsonElement("desktopImage")]
    public PhotoRef? DesktopImage { get; set; }

    [BsonElement("mobileImage")]
    public PhotoRef? MobileImage { get; set; }

    [BsonElement("altText")]
    public string? AltText { get; set; }

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }

    [BsonElement("enabled")]
    public bool Enabled { get; set; } = true;
}
