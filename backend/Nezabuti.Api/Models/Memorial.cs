using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nezabuti.Api.Models.Blocks;

namespace Nezabuti.Api.Models;

public class Memorial
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("publicId")]
    public string PublicId { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("mainPhoto")]
    public PhotoRef? MainPhoto { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public MemorialStatus Status { get; set; } = MemorialStatus.Draft;

    [BsonElement("privacy")]
    [BsonRepresentation(BsonType.String)]
    public MemorialPrivacy Privacy { get; set; } = MemorialPrivacy.Public;

    [BsonElement("blocks")]
    public List<MemorialBlock> Blocks { get; set; } = [];

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [BsonElement("publishedAt")]
    public DateTime? PublishedAt { get; set; }

    [BsonElement("archivedAt")]
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    /// Optional hero metadata (not required for publish).
    /// </summary>
    [BsonElement("callsign")]
    public string? Callsign { get; set; }

    [BsonElement("lifePeriod")]
    public string? LifePeriod { get; set; }

    [BsonElement("shortText")]
    public string? ShortText { get; set; }
}
