using MongoDB.Bson.Serialization.Attributes;

namespace Nezabuti.Api.Models;

/// <summary>
/// Singleton application/public site settings.
/// </summary>
public class SiteSettings
{
    public const string SingletonId = "site";

    [BsonId]
    public string Id { get; set; } = SingletonId;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("telegram")]
    public string Telegram { get; set; } = string.Empty;

    [BsonElement("viber")]
    public string Viber { get; set; } = string.Empty;

    // --- Commercial ---
    [BsonElement("additionalUpdatePrice")]
    public decimal AdditionalUpdatePrice { get; set; } = 250m;

    [BsonElement("qrSize50PriceDelta")]
    public decimal QrSize50PriceDelta { get; set; }

    [BsonElement("qrSize75PriceDelta")]
    public decimal QrSize75PriceDelta { get; set; } = 100m;

    [BsonElement("qrSize100PriceDelta")]
    public decimal QrSize100PriceDelta { get; set; } = 200m;

    // --- Technical content limits (not plan limits) ---
    [BsonElement("shortTextMaxChars")]
    public int ShortTextMaxChars { get; set; } = 2000;

    [BsonElement("textBlockMaxChars")]
    public int TextBlockMaxChars { get; set; } = 20000;

    [BsonElement("quoteMaxChars")]
    public int QuoteMaxChars { get; set; } = 1000;

    [BsonElement("timelineDescriptionMaxChars")]
    public int TimelineDescriptionMaxChars { get; set; } = 5000;

    [BsonElement("memoryTextMaxChars")]
    public int MemoryTextMaxChars { get; set; } = 10000;

    [BsonElement("serviceDescriptionMaxChars")]
    public int ServiceDescriptionMaxChars { get; set; } = 10000;

    [BsonElement("awardDescriptionMaxChars")]
    public int AwardDescriptionMaxChars { get; set; } = 5000;

    [BsonElement("photoCaptionMaxChars")]
    public int PhotoCaptionMaxChars { get; set; } = 1000;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
