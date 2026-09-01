using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nezabuti.Api.Models;

public class Plan
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

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

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
