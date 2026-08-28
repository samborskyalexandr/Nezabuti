using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Nezabuti.Api.Models;
using Nezabuti.Api.Models.Blocks;

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

    public MemorialPrivacy Privacy { get; set; } = MemorialPrivacy.Public;
    public string? Callsign { get; set; }
    public string? LifePeriod { get; set; }
    public string? ShortText { get; set; }
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
    public string? Callsign { get; set; }
    public string? LifePeriod { get; set; }
    public string? ShortText { get; set; }
    public string? MainPhotoId { get; set; }
    public List<MemorialBlockDto> Blocks { get; set; } = [];
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string? MainPhotoPreviewUrl { get; set; }
    public string? MainPhotoThumbUrl { get; set; }
}

public class MemorialAdminDto
{
    public string Id { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public PhotoRefDto? MainPhoto { get; set; }
    public MemorialStatus Status { get; set; }
    public MemorialPrivacy Privacy { get; set; }
    public List<MemorialBlockDto> Blocks { get; set; } = [];
    public string? Callsign { get; set; }
    public string? LifePeriod { get; set; }
    public string? ShortText { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
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
}

public class UpdateSiteSettingsRequest
{
    public string? Phone { get; set; }
    public string? Telegram { get; set; }
    public string? Viber { get; set; }
}
