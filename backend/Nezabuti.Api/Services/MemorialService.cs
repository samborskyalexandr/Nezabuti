using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Nezabuti.Api.Configuration;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Models.Blocks;
using Nezabuti.Api.Repositories;

namespace Nezabuti.Api.Services;

public interface IMemorialService
{
    Task<MemorialAdminDto> CreateAsync(CreateMemorialRequest request, CancellationToken ct = default);
    Task<PagedResult<MemorialListItemDto>> ListAsync(string? search, MemorialStatus? status, bool? isDemo, int page, int pageSize, CancellationToken ct = default);
    Task<MemorialAdminDto?> GetAdminAsync(string id, CancellationToken ct = default);
    Task<MemorialAdminDto?> UpdateAsync(string id, UpdateMemorialRequest request, CancellationToken ct = default);
    Task<MemorialAdminDto?> AssignPlanAsync(string id, AssignPlanRequest request, CancellationToken ct = default);
    Task<MemorialAdminDto?> AdjustUpdatesAsync(string id, AdjustUpdatesRequest request, CancellationToken ct = default);
    Task<MemorialAdminDto?> UpdatePaymentAsync(string id, UpdatePaymentRequest request, CancellationToken ct = default);
    Task<MemorialAdminDto?> ReorderBlocksAsync(string id, ReorderBlocksRequest request, CancellationToken ct = default);
    Task<MemorialAdminDto?> PublishAsync(string id, CancellationToken ct = default);
    Task<MemorialAdminDto?> ArchiveAsync(string id, CancellationToken ct = default);
    Task<MemorialAdminDto?> RestoreAsync(string id, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> PermanentDeleteAsync(string id, CancellationToken ct = default);
    Task<PublicMemorialDto?> GetPublicAsync(string publicId, CancellationToken ct = default);
    Task<PublicMemorialDto?> GetAdminPreviewAsync(string id, CancellationToken ct = default);
    Task<PhotoRefDto?> UploadPhotoAsync(string id, Stream stream, string contentType, bool asMainPhoto, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> DeletePhotoAsync(string id, string photoId, CancellationToken ct = default);
}

public sealed class MemorialService : IMemorialService
{
    private readonly IMemorialRepository _repo;
    private readonly IPhotoService _photos;
    private readonly IStatisticsService _stats;
    private readonly IPlanRepository _plans;
    private readonly IPlanLimitService _planLimits;
    private readonly ISiteSettingsRepository _settings;
    private readonly AppPublicSettings _app;

    public MemorialService(
        IMemorialRepository repo,
        IPhotoService photos,
        IStatisticsService stats,
        IPlanRepository plans,
        IPlanLimitService planLimits,
        ISiteSettingsRepository settings,
        IOptions<AppPublicSettings> app)
    {
        _repo = repo;
        _photos = photos;
        _stats = stats;
        _plans = plans;
        _planLimits = planLimits;
        _settings = settings;
        _app = app.Value;
    }

    public async Task<MemorialAdminDto> CreateAsync(CreateMemorialRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.PlanId))
        {
            throw new InvalidOperationException("Потрібно обрати тарифний план.");
        }

        var plan = await _plans.GetByIdAsync(request.PlanId.Trim(), ct);
        if (plan is null || !plan.IsActive)
        {
            throw new InvalidOperationException("Обраний план недоступний.");
        }

        var snapshot = _planLimits.CreateSnapshot(plan);
        if (plan.IsCustom && request.CustomOverrides is not null)
        {
            _planLimits.ApplyCustomOverrides(snapshot, request.CustomOverrides);
        }

        var memorial = await _repo.CreateAsync(request, snapshot, ct);
        var settings = await _settings.GetAsync(ct);
        memorial.QrPriceDeltaSnapshot = MemorialPricing.GetQrPriceDelta(settings, memorial.QrPlateSize);
        memorial.FinalPrice = MemorialPricing.CalculatePrice(snapshot.Price, memorial.QrPriceDeltaSnapshot);
        memorial.IsFinalPriceOverridden = false;
        memorial.PaymentStatus = PaymentStatus.Unpaid;
        memorial.PaidAt = null;
        await _repo.ReplaceAsync(memorial, ct);

        await _stats.EnsureExistsAsync(memorial, ct);
        return MapAdmin(memorial);
    }

    public async Task<PagedResult<MemorialListItemDto>> ListAsync(
        string? search,
        MemorialStatus? status,
        bool? isDemo,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (items, total) = await _repo.ListAsync(search, status, isDemo, page, pageSize, ct);
        return new PagedResult<MemorialListItemDto>
        {
            Items = items.Select(MapListItem).ToList(),
            Total = total,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 100)
        };
    }

    public async Task<MemorialAdminDto?> GetAdminAsync(string id, CancellationToken ct = default)
    {
        var memorial = await _repo.GetByIdAsync(id, ct);
        return memorial is null ? null : MapAdmin(memorial);
    }

    public async Task<MemorialAdminDto?> UpdateAsync(string id, UpdateMemorialRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null)
        {
            return null;
        }

        var previousQrSize = existing.QrPlateSize;
        var settings = await _settings.GetAsync(ct);

        if (request.FinalPrice.HasValue && request.FinalPrice.Value < 0)
        {
            throw new InvalidOperationException("Фінальна вартість не може бути від'ємною.");
        }

        var memorial = await _repo.UpdateAsync(id, request, ct);
        if (memorial is null)
        {
            return null;
        }

        var qrChanged = request.QrPlateSize.HasValue && request.QrPlateSize.Value != previousQrSize;
        if (qrChanged)
        {
            memorial.QrPriceDeltaSnapshot = MemorialPricing.GetQrPriceDelta(settings, memorial.QrPlateSize);
            if (!memorial.IsFinalPriceOverridden && memorial.PlanSnapshot is not null)
            {
                memorial.FinalPrice = MemorialPricing.CalculatePrice(
                    memorial.PlanSnapshot.Price,
                    memorial.QrPriceDeltaSnapshot);
            }

            await _repo.ReplaceAsync(memorial, ct);
        }
        else if (request.IsFinalPriceOverridden == true && request.FinalPrice.HasValue)
        {
            // already persisted in UpdateAsync
        }
        else if (request.IsFinalPriceOverridden == false
                 && memorial.PlanSnapshot is not null
                 && !request.FinalPrice.HasValue)
        {
            memorial.FinalPrice = MemorialPricing.CalculatePrice(
                memorial.PlanSnapshot.Price,
                memorial.QrPriceDeltaSnapshot);
            await _repo.ReplaceAsync(memorial, ct);
        }

        if (memorial.PlanSnapshot is not null)
        {
            var (ok, error) = _planLimits.ValidateMemorialAgainstPlan(memorial, memorial.PlanSnapshot);
            if (!ok)
            {
                await RestorePreviousAsync(existing, ct);
                throw new InvalidOperationException(error ?? "Меморіал не відповідає лімітам плану.");
            }
        }

        var contentCheck = ContentLimitValidator.Validate(memorial, settings);
        if (!contentCheck.Ok)
        {
            await RestorePreviousAsync(existing, ct);
            throw new InvalidOperationException(contentCheck.Error ?? "Перевищено технічні ліміти вмісту.");
        }

        return MapAdmin(memorial);
    }

    public async Task<MemorialAdminDto?> AssignPlanAsync(string id, AssignPlanRequest request, CancellationToken ct = default)
    {
        var memorial = await _repo.GetByIdAsync(id, ct);
        if (memorial is null)
        {
            return null;
        }

        var plan = await _plans.GetByIdAsync(request.PlanId.Trim(), ct);
        if (plan is null || !plan.IsActive)
        {
            throw new InvalidOperationException("Обраний план недоступний.");
        }

        var snapshot = _planLimits.CreateSnapshot(plan);
        if (plan.IsCustom && request.CustomOverrides is not null)
        {
            _planLimits.ApplyCustomOverrides(snapshot, request.CustomOverrides);
        }

        var (ok, error) = _planLimits.ValidateMemorialAgainstPlan(memorial, snapshot);
        if (!ok)
        {
            throw new InvalidOperationException(error ?? "Поточний вміст не вміщується в новий план.");
        }

        memorial = await _repo.UpdatePlanSnapshotAsync(id, snapshot, ct);
        if (memorial is null)
        {
            return null;
        }

        if (!memorial.IsFinalPriceOverridden)
        {
            memorial.FinalPrice = MemorialPricing.CalculatePrice(snapshot.Price, memorial.QrPriceDeltaSnapshot);
            await _repo.ReplaceAsync(memorial, ct);
        }

        return MapAdmin(memorial);
    }

    public async Task<MemorialAdminDto?> AdjustUpdatesAsync(string id, AdjustUpdatesRequest request, CancellationToken ct = default)
    {
        if (request.Delta is not (1 or -1))
        {
            throw new InvalidOperationException("Зміна лічильника оновлень дозволена лише на +1 або −1.");
        }

        var memorial = await _repo.AdjustUsedUpdatesAsync(id, request.Delta, ct);
        return memorial is null ? null : MapAdmin(memorial);
    }

    public async Task<MemorialAdminDto?> UpdatePaymentAsync(string id, UpdatePaymentRequest request, CancellationToken ct = default)
    {
        DateTime? paidAt = request.PaymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : null;
        var memorial = await _repo.UpdatePaymentAsync(id, request.PaymentStatus, paidAt, ct);
        return memorial is null ? null : MapAdmin(memorial);
    }

    public async Task<MemorialAdminDto?> ReorderBlocksAsync(string id, ReorderBlocksRequest request, CancellationToken ct = default)
    {
        var memorial = await _repo.ReorderBlocksAsync(id, request.BlockIds, ct);
        return memorial is null ? null : MapAdmin(memorial);
    }

    public async Task<MemorialAdminDto?> PublishAsync(string id, CancellationToken ct = default)
    {
        var memorial = await _repo.GetByIdAsync(id, ct);
        if (memorial is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(memorial.FullName) || memorial.MainPhoto is null)
        {
            throw new InvalidOperationException("Для публікації потрібні ПІБ та головне фото.");
        }

        if (memorial.Status == MemorialStatus.Archived)
        {
            throw new InvalidOperationException("Спочатку відновіть меморіал з архіву.");
        }

        memorial = await _repo.SetStatusAsync(id, MemorialStatus.Published, ct);
        return memorial is null ? null : MapAdmin(memorial);
    }

    public async Task<MemorialAdminDto?> ArchiveAsync(string id, CancellationToken ct = default)
    {
        var memorial = await _repo.GetByIdAsync(id, ct);
        if (memorial is null)
        {
            return null;
        }

        memorial = await _repo.SetStatusAsync(id, MemorialStatus.Archived, ct);
        return memorial is null ? null : MapAdmin(memorial);
    }

    public async Task<MemorialAdminDto?> RestoreAsync(string id, CancellationToken ct = default)
    {
        var memorial = await _repo.GetByIdAsync(id, ct);
        if (memorial is null)
        {
            return null;
        }

        if (memorial.Status != MemorialStatus.Archived)
        {
            throw new InvalidOperationException("Відновити можна лише архівні меморіали.");
        }

        memorial = await _repo.SetStatusAsync(id, MemorialStatus.Draft, ct);
        return memorial is null ? null : MapAdmin(memorial);
    }

    public async Task<(bool Ok, string? Error)> PermanentDeleteAsync(string id, CancellationToken ct = default)
    {
        var memorial = await _repo.GetByIdAsync(id, ct);
        if (memorial is null)
        {
            return (false, "Not found");
        }

        if (memorial.Status != MemorialStatus.Archived)
        {
            return (false, "Остаточне видалення дозволене лише для архівних меморіалів.");
        }

        var deleted = await _repo.DeleteArchivedAsync(id, ct);
        if (!deleted)
        {
            return (false, "Delete failed");
        }

        await _stats.DeleteByMemorialIdAsync(id, ct);
        await _photos.DeleteMemorialDirectoryAsync(memorial.PublicId, ct);
        return (true, null);
    }

    public async Task<PublicMemorialDto?> GetPublicAsync(string publicId, CancellationToken ct = default)
    {
        var memorial = await _repo.GetByPublicIdAsync(publicId, ct);
        if (memorial is null || memorial.Status != MemorialStatus.Published)
        {
            return null;
        }

        return MapPublic(memorial);
    }

    public async Task<PublicMemorialDto?> GetAdminPreviewAsync(string id, CancellationToken ct = default)
    {
        var memorial = await _repo.GetByIdAsync(id, ct);
        return memorial is null ? null : MapPublic(memorial, includeEmptyBlocks: false, forAdminPreview: true);
    }

    public async Task<PhotoRefDto?> UploadPhotoAsync(
        string id,
        Stream stream,
        string contentType,
        bool asMainPhoto,
        CancellationToken ct = default)
    {
        var memorial = await _repo.GetByIdAsync(id, ct);
        if (memorial is null)
        {
            return null;
        }

        var photo = await _photos.ProcessUploadAsync(memorial.PublicId, stream, contentType, ct);
        if (asMainPhoto)
        {
            var previous = memorial.MainPhoto;
            await _repo.UpdateMainPhotoAsync(id, photo, ct);
            if (previous is not null && previous.PhotoId != photo.PhotoId)
            {
                await _photos.DeletePhotoFilesAsync(memorial.PublicId, previous.PhotoId, ct);
            }
        }

        return MapPhoto(photo);
    }

    public async Task<(bool Ok, string? Error)> DeletePhotoAsync(string id, string photoId, CancellationToken ct = default)
    {
        var memorial = await _repo.GetByIdAsync(id, ct);
        if (memorial is null)
        {
            return (false, "Not found");
        }

        if (memorial.MainPhoto?.PhotoId == photoId)
        {
            await _repo.UpdateMainPhotoAsync(id, null, ct);
        }

        await _photos.DeletePhotoFilesAsync(memorial.PublicId, photoId, ct);
        return (true, null);
    }

    private async Task RestorePreviousAsync(Memorial previous, CancellationToken ct)
    {
        var restoreRequest = new UpdateMemorialRequest
        {
            FullName = previous.FullName,
            Privacy = previous.Privacy,
            IsDemo = previous.IsDemo,
            Callsign = previous.Callsign,
            LifePeriod = previous.LifePeriod,
            ShortText = previous.ShortText,
            MainPhotoId = previous.MainPhoto?.PhotoId,
            QrPlateSize = previous.QrPlateSize,
            FinalPrice = previous.FinalPrice,
            IsFinalPriceOverridden = previous.IsFinalPriceOverridden,
            Blocks = previous.Blocks.OrderBy(b => b.Order).Select(MapBlock).ToList()
        };
        await _repo.UpdateAsync(previous.Id, restoreRequest, ct);

        // Restore payment/pricing fields that UpdateAsync may not fully revert for QR delta.
        previous.UpdatedAt = DateTime.UtcNow;
        await _repo.ReplaceAsync(previous, ct);
    }

    private MemorialListItemDto MapListItem(Memorial m) => new()
    {
        Id = m.Id,
        PublicId = m.PublicId,
        FullName = m.FullName,
        Status = m.Status,
        Privacy = m.Privacy,
        IsDemo = m.IsDemo,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt,
        PublishedAt = m.PublishedAt,
        ArchivedAt = m.ArchivedAt,
        MainPhotoPreviewUrl = m.MainPhoto is null ? null : ToMediaUrl(m.MainPhoto.PreviewPath),
        MainPhotoThumbUrl = m.MainPhoto is null ? null : ToMediaUrl(
            string.IsNullOrWhiteSpace(m.MainPhoto.ThumbPath) ? m.MainPhoto.PreviewPath : m.MainPhoto.ThumbPath),
        PlanName = m.PlanSnapshot?.Name,
        PlanCode = m.PlanSnapshot?.Code,
        PaymentStatus = m.PaymentStatus,
        FinalPrice = MemorialPricing.ResolveFinalPrice(m)
    };

    private MemorialAdminDto MapAdmin(Memorial m) => new()
    {
        Id = m.Id,
        PublicId = m.PublicId,
        PublicUrl = $"{_app.PublicBaseUrl.TrimEnd('/')}/m/{m.PublicId}",
        FullName = m.FullName,
        MainPhoto = m.MainPhoto is null ? null : MapPhoto(m.MainPhoto),
        Status = m.Status,
        Privacy = m.Privacy,
        IsDemo = m.IsDemo,
        Blocks = m.Blocks.OrderBy(b => b.Order).Select(MapBlock).ToList(),
        Callsign = m.Callsign,
        LifePeriod = m.LifePeriod,
        ShortText = m.ShortText,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt,
        PublishedAt = m.PublishedAt,
        ArchivedAt = m.ArchivedAt,
        PlanSnapshot = m.PlanSnapshot is null ? null : MapSnapshot(m.PlanSnapshot),
        UsedUpdates = m.UsedUpdates,
        QrPlateSize = m.QrPlateSize,
        QrPriceDeltaSnapshot = m.QrPriceDeltaSnapshot,
        CalculatedPrice = MemorialPricing.ResolveCalculatedPrice(m),
        FinalPrice = MemorialPricing.ResolveFinalPrice(m),
        IsFinalPriceOverridden = m.IsFinalPriceOverridden,
        PaymentStatus = m.PaymentStatus,
        PaidAt = m.PaidAt,
        Usage = _planLimits.GetUsage(m)
    };

    private static PlanSnapshotDto MapSnapshot(PlanSnapshot s) => new()
    {
        PlanId = s.PlanId,
        Code = s.Code,
        Name = s.Name,
        Price = s.Price,
        IsCustom = s.IsCustom,
        IsUnlimited = s.IsUnlimited,
        MaxBlocks = s.MaxBlocks,
        MaxGalleryBlocks = s.MaxGalleryBlocks,
        MaxPhotosPerGallery = s.MaxPhotosPerGallery,
        MaxTimelineEvents = s.MaxTimelineEvents,
        MaxMemories = s.MaxMemories,
        IncludedUpdates = s.IncludedUpdates,
        SnapshotAt = s.SnapshotAt
    };

    private PublicMemorialDto MapPublic(Memorial m, bool includeEmptyBlocks = false, bool forAdminPreview = false)
    {
        var baseUrl = _app.PublicBaseUrl.TrimEnd('/');
        var canonical = $"{baseUrl}/m/{m.PublicId}";
        var description = string.IsNullOrWhiteSpace(m.ShortText)
            ? $"Меморіальна сторінка — {m.FullName}"
            : m.ShortText!;

        var blocks = m.Blocks
            .OrderBy(b => b.Order)
            .Where(b => includeEmptyBlocks || !IsEmptyBlock(b))
            .Select(MapBlock)
            .ToList();

        return new PublicMemorialDto
        {
            PublicId = m.PublicId,
            FullName = m.FullName,
            MainPhoto = m.MainPhoto is null ? null : MapPhoto(m.MainPhoto),
            Privacy = m.Privacy,
            IsDemo = m.IsDemo,
            Blocks = blocks,
            Callsign = m.Callsign,
            LifePeriod = m.LifePeriod,
            ShortText = m.ShortText,
            PublishedAt = m.PublishedAt,
            Seo = new SeoMetaDto
            {
                Title = forAdminPreview ? $"{m.FullName} (перегляд)" : m.FullName,
                Description = description,
                CanonicalUrl = canonical,
                OgImageUrl = m.MainPhoto is null ? null : AbsoluteMediaUrl(m.MainPhoto.FullPath),
                Robots = MemorialSeo.Robots(m.IsDemo, m.Privacy, forAdminPreview)
            }
        };
    }

    private static MemorialBlockDto MapBlock(MemorialBlock b) => new()
    {
        Id = b.Id,
        Type = b.Type,
        Order = b.Order,
        Data = JsonDocument.Parse(b.Data.ToJson()).RootElement.Clone()
    };

    private PhotoRefDto MapPhoto(PhotoRef photo)
    {
        var thumbPath = string.IsNullOrWhiteSpace(photo.ThumbPath) ? photo.PreviewPath : photo.ThumbPath;
        return new PhotoRefDto
        {
            PhotoId = photo.PhotoId,
            ThumbUrl = ToMediaUrl(thumbPath),
            PreviewUrl = ToMediaUrl(photo.PreviewPath),
            FullUrl = ToMediaUrl(photo.FullPath),
            Width = photo.Width,
            Height = photo.Height
        };
    }

    private string ToMediaUrl(string relativePath)
        => $"/uploads/{relativePath.TrimStart('/')}";

    private string AbsoluteMediaUrl(string relativePath)
    {
        var baseUrl = _app.PublicBaseUrl.TrimEnd('/');
        return $"{baseUrl}{ToMediaUrl(relativePath)}";
    }

    private static bool IsEmptyBlock(MemorialBlock block)
    {
        if (block.Data.ElementCount == 0)
        {
            return true;
        }

        return block.Type switch
        {
            BlockType.Text => !block.Data.Contains("html") || string.IsNullOrWhiteSpace(block.Data["html"].AsString),
            BlockType.Quote => !block.Data.Contains("text") || string.IsNullOrWhiteSpace(block.Data["text"].AsString),
            BlockType.Timeline => !block.Data.Contains("events") || !block.Data["events"].IsBsonArray || block.Data["events"].AsBsonArray.Count == 0,
            BlockType.Gallery => !block.Data.Contains("items") || !block.Data["items"].IsBsonArray || block.Data["items"].AsBsonArray.Count == 0,
            BlockType.Image => IsImageEmpty(block.Data),
            BlockType.Awards => !block.Data.Contains("items") || !block.Data["items"].IsBsonArray || block.Data["items"].AsBsonArray.Count == 0,
            BlockType.Memories => !block.Data.Contains("items") || !block.Data["items"].IsBsonArray || block.Data["items"].AsBsonArray.Count == 0,
            BlockType.Service => IsServiceEmpty(block.Data),
            _ => false
        };
    }

    private static bool IsImageEmpty(BsonDocument data)
    {
        if (data.Contains("photoId") && !string.IsNullOrWhiteSpace(data["photoId"].AsString))
        {
            return false;
        }

        if (data.Contains("photo") && data["photo"].IsBsonDocument)
        {
            var photo = data["photo"].AsBsonDocument;
            return !photo.Contains("photoId") || string.IsNullOrWhiteSpace(photo["photoId"].AsString);
        }

        return true;
    }

    private static bool IsServiceEmpty(BsonDocument data)
    {
        string[] keys = ["callsign", "rank", "unit", "servicePeriod", "description"];
        return keys.All(k => !data.Contains(k) || string.IsNullOrWhiteSpace(data[k].ToString()));
    }
}
