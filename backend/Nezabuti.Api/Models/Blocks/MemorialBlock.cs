using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nezabuti.Api.Models.Blocks;

public class MemorialBlock
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("order")]
    public int Order { get; set; }

    /// <summary>
    /// Type-specific payload. Kept as BsonDocument so new block types
    /// can be added without rewriting the Memorial model.
    /// </summary>
    [BsonElement("data")]
    public BsonDocument Data { get; set; } = new();
}
