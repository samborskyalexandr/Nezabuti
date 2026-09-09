using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nezabuti.Api.Models;

public enum MemorialPaymentType
{
    Initial = 0,
    Renewal = 1
}

public enum MemorialPaymentMethod
{
    Manual = 0
}

public class MemorialPayment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("memorialId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MemorialId { get; set; } = string.Empty;

    [BsonElement("customerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CustomerId { get; set; }

    [BsonElement("amount")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    [BsonElement("paidAt")]
    public DateTime PaidAt { get; set; }

    [BsonElement("periodFrom")]
    public DateTime PeriodFrom { get; set; }

    [BsonElement("periodTo")]
    public DateTime PeriodTo { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public MemorialPaymentType Type { get; set; }

    [BsonElement("method")]
    [BsonRepresentation(BsonType.String)]
    public MemorialPaymentMethod Method { get; set; } = MemorialPaymentMethod.Manual;

    [BsonElement("note")]
    public string? Note { get; set; }

    [BsonElement("createdBy")]
    public string? CreatedBy { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
}
