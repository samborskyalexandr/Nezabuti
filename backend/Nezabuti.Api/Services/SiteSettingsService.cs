using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Repositories;

namespace Nezabuti.Api.Services;

public interface ISiteSettingsService
{
    Task<SiteSettingsDto> GetAsync(CancellationToken ct = default);
    Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken ct = default);
    Task<SiteSettingsDto> UpdateAsync(UpdateSiteSettingsRequest request, CancellationToken ct = default);
}

public sealed class SiteSettingsService : ISiteSettingsService
{
    private readonly ISiteSettingsRepository _repo;

    public SiteSettingsService(ISiteSettingsRepository repo)
    {
        _repo = repo;
    }

    public async Task<SiteSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await _repo.GetAsync(ct);
        return Map(settings);
    }

    public async Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken ct = default)
    {
        var settings = await _repo.GetAsync(ct);
        return new PublicSiteSettingsDto
        {
            Phone = settings.Phone ?? string.Empty,
            Telegram = settings.Telegram ?? string.Empty,
            Viber = settings.Viber ?? string.Empty
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

        var settings = await _repo.UpsertAsync(current, ct);
        return Map(settings);
    }

    private static SiteSettingsDto Map(SiteSettings s) => new()
    {
        Phone = s.Phone ?? string.Empty,
        Telegram = s.Telegram ?? string.Empty,
        Viber = s.Viber ?? string.Empty,
        AdditionalUpdatePrice = s.AdditionalUpdatePrice,
        QrSize50PriceDelta = s.QrSize50PriceDelta,
        QrSize75PriceDelta = s.QrSize75PriceDelta,
        QrSize100PriceDelta = s.QrSize100PriceDelta,
        ShortTextMaxChars = s.ShortTextMaxChars,
        TextBlockMaxChars = s.TextBlockMaxChars,
        QuoteMaxChars = s.QuoteMaxChars,
        TimelineDescriptionMaxChars = s.TimelineDescriptionMaxChars,
        MemoryTextMaxChars = s.MemoryTextMaxChars,
        ServiceDescriptionMaxChars = s.ServiceDescriptionMaxChars,
        AwardDescriptionMaxChars = s.AwardDescriptionMaxChars,
        PhotoCaptionMaxChars = s.PhotoCaptionMaxChars
    };

    private static string Normalize(string? value) => (value ?? string.Empty).Trim();

    private static int ClampChars(int value) => Math.Clamp(value, 1, 500_000);
}
