using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Repositories;

namespace Nezabuti.Api.Services;

public interface ISiteSettingsService
{
    Task<SiteSettingsDto> GetAsync(CancellationToken ct = default);
    Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken ct = default);
    Task<SiteSettingsDto> UpdateAsync(UpdateSiteSettingsRequest request, CancellationToken ct = default);
    Task<TelegramTestResultDto> TestTelegramAsync(CancellationToken ct = default);
    Task<PhotoRefDto> UploadHomeImageAsync(Stream stream, string contentType, CancellationToken ct = default);
}

public sealed class SiteSettingsService : ISiteSettingsService
{
    private readonly ISiteSettingsRepository _repo;
    private readonly ISecretEncryptionService _secrets;
    private readonly ITelegramAdminNotifyService _telegram;
    private readonly IPhotoService _photos;

    public SiteSettingsService(
        ISiteSettingsRepository repo,
        ISecretEncryptionService secrets,
        ITelegramAdminNotifyService telegram,
        IPhotoService photos)
    {
        _repo = repo;
        _secrets = secrets;
        _telegram = telegram;
        _photos = photos;
    }

    public async Task<SiteSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await _repo.GetAsync(ct);
        return Map(settings);
    }

    public async Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken ct = default)
    {
        var settings = await _repo.GetAsync(ct);
        var (howItWorksEnabled, slides, demo) = HomeShowcaseMapper.ToPublic(settings.HomeShowcase, MapPhoto);
        return new PublicSiteSettingsDto
        {
            Phone = settings.Phone ?? string.Empty,
            Telegram = settings.Telegram ?? string.Empty,
            Viber = settings.Viber ?? string.Empty,
            HowItWorksEnabled = howItWorksEnabled,
            HowItWorksSlides = slides,
            Demo = demo
        };
    }

    public async Task<SiteSettingsDto> UpdateAsync(UpdateSiteSettingsRequest request, CancellationToken ct = default)
    {
        var current = await _repo.GetAsync(ct);

        if (request.Phone is not null)
        {
            current.Phone = Normalize(request.Phone);
        }

        if (request.Telegram is not null)
        {
            current.Telegram = Normalize(request.Telegram);
        }

        if (request.Viber is not null)
        {
            current.Viber = Normalize(request.Viber);
        }

        if (request.AdditionalUpdatePrice.HasValue)
        {
            current.AdditionalUpdatePrice = request.AdditionalUpdatePrice.Value;
        }

        if (request.QrSize50PriceDelta.HasValue)
        {
            current.QrSize50PriceDelta = request.QrSize50PriceDelta.Value;
        }

        if (request.QrSize75PriceDelta.HasValue)
        {
            current.QrSize75PriceDelta = request.QrSize75PriceDelta.Value;
        }

        if (request.QrSize100PriceDelta.HasValue)
        {
            current.QrSize100PriceDelta = request.QrSize100PriceDelta.Value;
        }

        if (request.TelegramNotifyEnabled.HasValue)
        {
            current.TelegramNotifyEnabled = request.TelegramNotifyEnabled.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.TelegramBotToken))
        {
            current.TelegramBotTokenEnc = _secrets.Encrypt(request.TelegramBotToken.Trim());
        }

        if (request.TelegramChatId is not null)
        {
            current.TelegramChatId = string.IsNullOrWhiteSpace(request.TelegramChatId)
                ? null
                : request.TelegramChatId.Trim();
        }

        if (request.ShortTextMaxChars.HasValue)
        {
            current.ShortTextMaxChars = ClampChars(request.ShortTextMaxChars.Value);
        }

        if (request.TextBlockMaxChars.HasValue)
        {
            current.TextBlockMaxChars = ClampChars(request.TextBlockMaxChars.Value);
        }

        if (request.QuoteMaxChars.HasValue)
        {
            current.QuoteMaxChars = ClampChars(request.QuoteMaxChars.Value);
        }

        if (request.TimelineDescriptionMaxChars.HasValue)
        {
            current.TimelineDescriptionMaxChars = ClampChars(request.TimelineDescriptionMaxChars.Value);
        }

        if (request.MemoryTextMaxChars.HasValue)
        {
            current.MemoryTextMaxChars = ClampChars(request.MemoryTextMaxChars.Value);
        }

        if (request.ServiceDescriptionMaxChars.HasValue)
        {
            current.ServiceDescriptionMaxChars = ClampChars(request.ServiceDescriptionMaxChars.Value);
        }

        if (request.AwardDescriptionMaxChars.HasValue)
        {
            current.AwardDescriptionMaxChars = ClampChars(request.AwardDescriptionMaxChars.Value);
        }

        if (request.PhotoCaptionMaxChars.HasValue)
        {
            current.PhotoCaptionMaxChars = ClampChars(request.PhotoCaptionMaxChars.Value);
        }

        if (request.HomeShowcase is not null)
        {
            var previous = HomeShowcaseMapper.Normalize(current.HomeShowcase);
            var next = HomeShowcaseMapper.FromAdminRequest(request.HomeShowcase);
            next.SeedMobileRevision = current.HomeShowcase?.SeedMobileRevision;
            HomeShowcaseMapper.ValidateForSave(next);
            await CleanupUnusedHomeImagesAsync(previous, next, ct);
            current.HomeShowcase = next;
        }

        var settings = await _repo.UpsertAsync(current, ct);
        return Map(settings);
    }

    public async Task<PhotoRefDto> UploadHomeImageAsync(Stream stream, string contentType, CancellationToken ct = default)
    {
        var photo = await _photos.ProcessHomeUploadAsync(stream, contentType, ct);
        return MapPhoto(photo);
    }

    public async Task<TelegramTestResultDto> TestTelegramAsync(CancellationToken ct = default)
    {
        var (ok, message) = await _telegram.SendTestMessageAsync(ct);
        return new TelegramTestResultDto { Ok = ok, Message = message };
    }

    private SiteSettingsDto Map(SiteSettings s)
    {
        var hasToken = !string.IsNullOrWhiteSpace(s.TelegramBotTokenEnc);
        return new()
        {
            Phone = s.Phone ?? string.Empty,
            Telegram = s.Telegram ?? string.Empty,
            Viber = s.Viber ?? string.Empty,
            AdditionalUpdatePrice = s.AdditionalUpdatePrice,
            QrSize50PriceDelta = s.QrSize50PriceDelta,
            QrSize75PriceDelta = s.QrSize75PriceDelta,
            QrSize100PriceDelta = s.QrSize100PriceDelta,
            TelegramNotifyEnabled = s.TelegramNotifyEnabled,
            TelegramBotTokenMasked = hasToken ? _secrets.Mask(s.TelegramBotTokenEnc) : string.Empty,
            HasTelegramBotToken = hasToken,
            TelegramChatId = s.TelegramChatId,
            ShortTextMaxChars = s.ShortTextMaxChars,
            TextBlockMaxChars = s.TextBlockMaxChars,
            QuoteMaxChars = s.QuoteMaxChars,
            TimelineDescriptionMaxChars = s.TimelineDescriptionMaxChars,
            MemoryTextMaxChars = s.MemoryTextMaxChars,
            ServiceDescriptionMaxChars = s.ServiceDescriptionMaxChars,
            AwardDescriptionMaxChars = s.AwardDescriptionMaxChars,
            PhotoCaptionMaxChars = s.PhotoCaptionMaxChars,
            HomeShowcase = HomeShowcaseMapper.ToAdmin(s.HomeShowcase ?? new HomeShowcaseSettings(), MapPhoto)
        };
    }

    private async Task CleanupUnusedHomeImagesAsync(
        HomeShowcaseSettings previous,
        HomeShowcaseSettings next,
        CancellationToken ct)
    {
        var keep = new HashSet<string>(HomeShowcaseMapper.CollectPhotoIds(next), StringComparer.Ordinal);
        foreach (var photoId in HomeShowcaseMapper.CollectPhotoIds(previous))
        {
            if (!keep.Contains(photoId))
            {
                await _photos.DeleteHomePhotoFilesAsync(photoId, ct);
            }
        }
    }

    private static PhotoRefDto MapPhoto(PhotoRef photo)
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

    private static string ToMediaUrl(string relativePath)
        => $"/uploads/{relativePath.TrimStart('/')}";

    private static string Normalize(string? value) => (value ?? string.Empty).Trim();

    private static int ClampChars(int value) => Math.Clamp(value, 1, 500_000);
}
