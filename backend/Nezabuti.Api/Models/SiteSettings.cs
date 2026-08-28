using MongoDB.Bson.Serialization.Attributes;

namespace Nezabuti.Api.Models;

/// <summary>
/// Singleton application/public site settings (contacts).
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

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
