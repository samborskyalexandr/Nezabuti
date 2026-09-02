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
    public decimal? Price { get; set; }
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

    public List<MemorialBlockDto> Blocks { get; set; } = [];
}

public class UpdatePaymentRequest
{
    public PaymentStatus PaymentStatus { get; set; }
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
    public PlanUsageDto? Usage { get; set; }
}

public class PlanSnapshotDto
{
    public string PlanId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
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
    public decimal Price { get; set; }
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
    public decimal Price { get; set; }
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
    public decimal Price { get; set; }
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
