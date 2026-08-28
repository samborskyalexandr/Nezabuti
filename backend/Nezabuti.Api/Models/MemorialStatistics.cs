using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nezabuti.Api.Models;

public class MemorialStatistics
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("memorialId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MemorialId { get; set; } = string.Empty;

    [BsonElement("publicId")]
    public string PublicId { get; set; } = string.Empty;

    [BsonElement("totalViews")]
    public long TotalViews { get; set; }

    [BsonElement("lastViewedAt")]
    public DateTime? LastViewedAt { get; set; }

    [BsonElement("viewsPerDay")]
    public List<DailyViewCount> ViewsPerDay { get; set; } = [];
}

public class DailyViewCount
{
    [BsonElement("date")]
    public string Date { get; set; } = string.Empty; // yyyy-MM-dd UTC

    [BsonElement("count")]
    public long Count { get; set; }
}
