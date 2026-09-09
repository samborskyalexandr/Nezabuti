using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Nezabuti.Api.Models;
using Nezabuti.Api.Models.Blocks;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.DTOs;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class CreateMemorialRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string PlanId { get; set; } = string.Empty;

    public MemorialPrivacy Privacy { get; set; } = MemorialPrivacy.Public;

    /// <summary>Advertising / presentation memorial. Default false.</summary>
    public bool IsDemo { get; set; }

    public string? Callsign { get; set; }
    public string? LifePeriod { get; set; }
    public string? ShortText { get; set; }

    /// <summary>Optional per-memorial overrides when assigning Custom plan.</summary>
    public CustomPlanOverridesDto? CustomOverrides { get; set; }
}

public class CustomPlanOverridesDto
{
    /// <summary>Legacy alias for InitialPrice.</summary>
    public decimal? Price { get; set; }
    public decimal? InitialPrice { get; set; }
    public decimal? RenewalPrice { get; set; }
    public bool? IsUnlimited { get; set; }
    public int? MaxBlocks { get; set; }
    public int? MaxGalleryBlocks { get; set; }
    public int? MaxPhotosPerGallery { get; set; }
    public int? MaxTimelineEvents { get; set; }
    public int? MaxMemories { get; set; }
    public int? IncludedUpdates { get; set; }
}

/// <summary>
/// Update DTO intentionally omits PublicId — it is immutable after creation.
/// </summary>
public class UpdateMemorialRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public MemorialPrivacy Privacy { get; set; } = MemorialPrivacy.Public;

    /// <summary>Advertising / presentation memorial.</summary>
    public bool IsDemo { get; set; }

    public string? Callsign { get; set; }
    public string? LifePeriod { get; set; }
    public string? ShortText { get; set; }
    public string? MainPhotoId { get; set; }
    public QrPlateSize? QrPlateSize { get; set; }

    /// <summary>
    /// When set, updates FinalPrice. Use with IsFinalPriceOverridden.
    /// </summary>
    public decimal? FinalPrice { get; set; }

    public bool? IsFinalPriceOverridden { get; set; }

    public string? CustomerId { get; set; }

    public List<MemorialBlockDto> Blocks { get; set; } = [];
}

public class UpdatePaymentRequest
{
    public PaymentStatus PaymentStatus { get; set; }
}

public class ConfirmPaymentRequest
{
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
    /// <summary>Initial or Renewal (optional when using dedicated endpoints).</summary>
    public MemorialPaymentType? Type { get; set; }
}

public enum BillingFilter
{
    EndingIn30 = 0,
    EndingIn7 = 1,
    Grace = 2,
    Expired = 3,
    Suspended = 4
}

public class MemorialBlockDto
{
    public string? Id { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    public int Order { get; set; }

    public JsonElement Data { get; set; }
}

public class ReorderBlocksRequest
{
    [Required]
    [MinLength(1)]
    public List<string> BlockIds { get; set; } = [];
}

public class MemorialListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public MemorialStatus Status { get; set; }
    public MemorialPrivacy Privacy { get; set; }
    public bool IsDemo { get; set; }
    public string? CustomerId { get; set; }
    public CustomerSummaryDto? Customer { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string? MainPhotoPreviewUrl { get; set; }
    public string? MainPhotoThumbUrl { get; set; }
    public string? PlanName { get; set; }
    public string? PlanCode { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public decimal? FinalPrice { get; set; }
    public DateTime? PaidUntil { get; set; }
    public DateTime? GraceUntil { get; set; }
    public DateTime? LastPaymentAt { get; set; }
    public PaymentState PaymentState { get; set; } = PaymentState.Unconfigured;
    public string PaymentStateLabel { get; set; } = string.Empty;
}

public class MemorialAdminDto
{
    public string Id { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    /// <summary>Canonical public page URL encoded in the QR code.</summary>
    public string PublicUrl { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public PhotoRefDto? MainPhoto { get; set; }
    public MemorialStatus Status { get; set; }
    public MemorialPrivacy Privacy { get; set; }
    public bool IsDemo { get; set; }
    public string? CustomerId { get; set; }
    public CustomerSummaryDto? Customer { get; set; }
    public List<MemorialBlockDto> Blocks { get; set; } = [];
    public string? Callsign { get; set; }
    public string? LifePeriod { get; set; }
    public string? ShortText { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public PlanSnapshotDto? PlanSnapshot { get; set; }
    public int UsedUpdates { get; set; }
    public QrPlateSize QrPlateSize { get; set; } = QrPlateSize.Size50;
    public decimal QrPriceDeltaSnapshot { get; set; }
    public decimal? CalculatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public bool IsFinalPriceOverridden { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public DateTime? PaidAt { get; set; }
    public DateTime? LastPaymentAt { get; set; }
    public DateTime? PaidUntil { get; set; }
    public DateTime? GraceUntil { get; set; }
    public PaymentState PaymentState { get; set; } = PaymentState.Unconfigured;
    public string PaymentStateLabel { get; set; } = string.Empty;
    public PlanUsageDto? Usage { get; set; }
}

public class PlanSnapshotDto
{
    public string PlanId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Legacy alias of InitialPrice.</summary>
    public decimal Price { get; set; }
    public decimal InitialPrice { get; set; }
    public decimal RenewalPrice { get; set; }
    public bool IsCustom { get; set; }
    public bool IsUnlimited { get; set; }
    public int? MaxBlocks { get; set; }
    public int? MaxGalleryBlocks { get; set; }
    public int? MaxPhotosPerGallery { get; set; }
    public int? MaxTimelineEvents { get; set; }
    public int? MaxMemories { get; set; }
    public int IncludedUpdates { get; set; }
    public DateTime SnapshotAt { get; set; }
}

public class PlanDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Legacy alias of InitialPrice.</summary>
    public decimal Price { get; set; }
    public decimal InitialPrice { get; set; }
    public decimal RenewalPrice { get; set; }
    public bool IsActive { get; set; }
    public bool IsCustom { get; set; }
    public bool IsUnlimited { get; set; }
    public int? MaxBlocks { get; set; }
    public int? MaxGalleryBlocks { get; set; }
    public int? MaxPhotosPerGallery { get; set; }
    public int? MaxTimelineEvents { get; set; }
    public int? MaxMemories { get; set; }
    public int IncludedUpdates { get; set; }
}

public class UpdatePlanRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    /// <summary>Legacy; used when InitialPrice is 0.</summary>
    public decimal Price { get; set; }
    public decimal InitialPrice { get; set; }
    public decimal RenewalPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsUnlimited { get; set; }
    public int? MaxBlocks { get; set; }
    public int? MaxGalleryBlocks { get; set; }
    public int? MaxPhotosPerGallery { get; set; }
    public int? MaxTimelineEvents { get; set; }
    public int? MaxMemories { get; set; }
    public int IncludedUpdates { get; set; }
}

public class AssignPlanRequest
{
    [Required]
    public string PlanId { get; set; } = string.Empty;

    public CustomPlanOverridesDto? CustomOverrides { get; set; }
}

public class AdjustUpdatesRequest
{
    /// <summary>+1 or -1</summary>
    [Range(-1, 1)]
    public int Delta { get; set; }
}

public class PublicPlanDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Legacy alias of InitialPrice.</summary>
    public decimal Price { get; set; }
    public decimal InitialPrice { get; set; }
    public decimal RenewalPrice { get; set; }
    public int? MaxGalleryBlocks { get; set; }
    public int? MaxPhotosPerGallery { get; set; }
    public int? MaxTimelineEvents { get; set; }
    public int? MaxMemories { get; set; }
    public int IncludedUpdates { get; set; }
    public bool IsRecommended { get; set; }
}

public class PhotoRefDto
{
    public string PhotoId { get; set; } = string.Empty;
    public string ThumbUrl { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public string FullUrl { get; set; } = string.Empty;
    public int? Width { get; set; }
    public int? Height { get; set; }
}

public class PublicMemorialDto
{
    public string PublicId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public PhotoRefDto? MainPhoto { get; set; }
    public MemorialPrivacy Privacy { get; set; }
    public bool IsDemo { get; set; }
    /// <summary>True when publication is Suspended — content blocks are omitted.</summary>
    public bool IsTemporarilyUnavailable { get; set; }
    public List<MemorialBlockDto> Blocks { get; set; } = [];
    public string? Callsign { get; set; }
    public string? LifePeriod { get; set; }
    public string? ShortText { get; set; }
    public DateTime? PublishedAt { get; set; }
    public SeoMetaDto Seo { get; set; } = new();
}

public class SeoMetaDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public string? OgImageUrl { get; set; }
    public string Robots { get; set; } = "index,follow";
}

public class MemorialStatisticsDto
{
    public string PublicId { get; set; } = string.Empty;
    public long TotalViews { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public List<DailyViewCountDto> ViewsPerDay { get; set; } = [];
}

public class DailyViewCountDto
{
    public string Date { get; set; } = string.Empty;
    public long Count { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public long Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class RecordViewRequest
{
    /// <summary>
    /// When true, the request is treated as admin preview and is not counted.
    /// </summary>
    public bool IsAdminPreview { get; set; }
}

public class HealthResponse
{
    public string Status { get; set; } = "ok";
    public string Service { get; set; } = "nezabuti-api";
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
}

public class SiteSettingsDto
{
    public string Phone { get; set; } = string.Empty;
    public string Telegram { get; set; } = string.Empty;
    public string Viber { get; set; } = string.Empty;
    public decimal AdditionalUpdatePrice { get; set; }
    public decimal QrSize50PriceDelta { get; set; }
    public decimal QrSize75PriceDelta { get; set; }
    public decimal QrSize100PriceDelta { get; set; }
    public bool TelegramNotifyEnabled { get; set; }
    public string TelegramBotTokenMasked { get; set; } = string.Empty;
    public bool HasTelegramBotToken { get; set; }
    public string? TelegramChatId { get; set; }
    public int ShortTextMaxChars { get; set; }
    public int TextBlockMaxChars { get; set; }
    public int QuoteMaxChars { get; set; }
    public int TimelineDescriptionMaxChars { get; set; }
    public int MemoryTextMaxChars { get; set; }
    public int ServiceDescriptionMaxChars { get; set; }
    public int AwardDescriptionMaxChars { get; set; }
    public int PhotoCaptionMaxChars { get; set; }
}

public class UpdateSiteSettingsRequest
{
    public string? Phone { get; set; }
    public string? Telegram { get; set; }
    public string? Viber { get; set; }
    public decimal? AdditionalUpdatePrice { get; set; }
    public decimal? QrSize50PriceDelta { get; set; }
    public decimal? QrSize75PriceDelta { get; set; }
    public decimal? QrSize100PriceDelta { get; set; }
    public bool? TelegramNotifyEnabled { get; set; }
    /// <summary>Write-only; omit or empty to keep existing token.</summary>
    public string? TelegramBotToken { get; set; }
    public string? TelegramChatId { get; set; }
    public int? ShortTextMaxChars { get; set; }
    public int? TextBlockMaxChars { get; set; }
    public int? QuoteMaxChars { get; set; }
    public int? TimelineDescriptionMaxChars { get; set; }
    public int? MemoryTextMaxChars { get; set; }
    public int? ServiceDescriptionMaxChars { get; set; }
    public int? AwardDescriptionMaxChars { get; set; }
    public int? PhotoCaptionMaxChars { get; set; }
}

public class PublicSiteSettingsDto
{
    public string Phone { get; set; } = string.Empty;
    public string Telegram { get; set; } = string.Empty;
    public string Viber { get; set; } = string.Empty;
}

public class CustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? TelegramUsername { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CustomerSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class CreateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? TelegramUsername { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class UpdateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? TelegramUsername { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class MemorialPaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string MemorialId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public MemorialPaymentType Type { get; set; }
    public MemorialPaymentMethod Method { get; set; }
    public string? Note { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TelegramTestResultDto
{
    public bool Ok { get; set; }
    public string Message { get; set; } = string.Empty;
}
