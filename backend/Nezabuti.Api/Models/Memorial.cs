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

    /// <summary>
    /// Advertising / presentation memorial. Missing field on legacy documents is treated as false.
    /// </summary>
    [BsonElement("isDemo")]
    public bool IsDemo { get; set; }

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

    /// <summary>
    /// Frozen plan terms. Null for legacy memorials until admin assigns a plan.
    /// </summary>
    [BsonElement("planSnapshot")]
    public PlanSnapshot? PlanSnapshot { get; set; }

    [BsonElement("usedUpdates")]
    public int UsedUpdates { get; set; }

    [BsonElement("qrPlateSize")]
    [BsonRepresentation(BsonType.String)]
    public QrPlateSize QrPlateSize { get; set; } = QrPlateSize.Size50;

    /// <summary>
    /// QR surcharge frozen when size was chosen / last saved. Protects old memorials from settings changes.
    /// </summary>
    [BsonElement("qrPriceDeltaSnapshot")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal QrPriceDeltaSnapshot { get; set; }

    [BsonElement("paymentStatus")]
    [BsonRepresentation(BsonType.String)]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    /// <summary>
    /// Actual amount due / agreed. May be manually overridden by admin.
    /// </summary>
    [BsonElement("finalPrice")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal? FinalPrice { get; set; }

    [BsonElement("isFinalPriceOverridden")]
    public bool IsFinalPriceOverridden { get; set; }

    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }
}
