using Nezabuti.Api.DTOs;
using Nezabuti.Api.Repositories;

namespace Nezabuti.Api.Services;

public interface ISiteSettingsService
{
    Task<SiteSettingsDto> GetAsync(CancellationToken ct = default);
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

    public async Task<SiteSettingsDto> UpdateAsync(UpdateSiteSettingsRequest request, CancellationToken ct = default)
    {
        var phone = Normalize(request.Phone);
        var telegram = Normalize(request.Telegram);
        var viber = Normalize(request.Viber);

        var settings = await _repo.UpsertAsync(phone, telegram, viber, ct);
        return Map(settings);
    }

    private static SiteSettingsDto Map(Models.SiteSettings s) => new()
    {
        Phone = s.Phone ?? string.Empty,
        Telegram = s.Telegram ?? string.Empty,
        Viber = s.Viber ?? string.Empty
    };

    private static string Normalize(string? value) => (value ?? string.Empty).Trim();
}
