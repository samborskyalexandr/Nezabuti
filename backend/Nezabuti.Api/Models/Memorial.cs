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

    [BsonElement("isDemo")]
    public bool IsDemo { get; set; }

    [BsonElement("customerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CustomerId { get; set; }

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

    [BsonElement("callsign")]
    public string? Callsign { get; set; }

    [BsonElement("lifePeriod")]
    public string? LifePeriod { get; set; }

    [BsonElement("shortText")]
    public string? ShortText { get; set; }

    [BsonElement("planSnapshot")]
    public PlanSnapshot? PlanSnapshot { get; set; }

    [BsonElement("usedUpdates")]
    public int UsedUpdates { get; set; }

    [BsonElement("qrPlateSize")]
    [BsonRepresentation(BsonType.String)]
    public QrPlateSize QrPlateSize { get; set; } = QrPlateSize.Size50;

    [BsonElement("qrPriceDeltaSnapshot")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal QrPriceDeltaSnapshot { get; set; }

    /// <summary>Legacy Unpaid/Paid flag — prefer computed PaymentState from dates.</summary>
    [BsonElement("paymentStatus")]
    [BsonRepresentation(BsonType.String)]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    [BsonElement("finalPrice")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal? FinalPrice { get; set; }

    [BsonElement("isFinalPriceOverridden")]
    public bool IsFinalPriceOverridden { get; set; }

    /// <summary>Legacy; prefer LastPaymentAt.</summary>
    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }

    [BsonElement("lastPaymentAt")]
    public DateTime? LastPaymentAt { get; set; }

    [BsonElement("paidUntil")]
    public DateTime? PaidUntil { get; set; }

    [BsonElement("graceUntil")]
    public DateTime? GraceUntil { get; set; }

    [BsonElement("reminder30SentAt")]
    public DateTime? Reminder30SentAt { get; set; }

    [BsonElement("reminder7SentAt")]
    public DateTime? Reminder7SentAt { get; set; }

    [BsonElement("expiredReminderSentAt")]
    public DateTime? ExpiredReminderSentAt { get; set; }

    [BsonElement("grace7ReminderSentAt")]
    public DateTime? Grace7ReminderSentAt { get; set; }

    [BsonElement("suspendedReminderSentAt")]
    public DateTime? SuspendedReminderSentAt { get; set; }
}
