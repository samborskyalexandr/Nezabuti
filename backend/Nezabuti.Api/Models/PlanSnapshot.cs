using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nezabuti.Api.Models;

/// <summary>
/// Frozen plan terms for a memorial at assignment time.
/// </summary>
public class PlanSnapshot
{
    [BsonElement("planId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PlanId { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("isCustom")]
    public bool IsCustom { get; set; }

    [BsonElement("isUnlimited")]
    public bool IsUnlimited { get; set; }

    [BsonElement("maxBlocks")]
    public int? MaxBlocks { get; set; }

    [BsonElement("maxGalleryBlocks")]
    public int? MaxGalleryBlocks { get; set; }

    [BsonElement("maxPhotosPerGallery")]
    public int? MaxPhotosPerGallery { get; set; }

    [BsonElement("maxTimelineEvents")]
    public int? MaxTimelineEvents { get; set; }

    [BsonElement("maxMemories")]
    public int? MaxMemories { get; set; }

    [BsonElement("includedUpdates")]
    public int IncludedUpdates { get; set; }

    [BsonElement("snapshotAt")]
    public DateTime SnapshotAt { get; set; }
}
