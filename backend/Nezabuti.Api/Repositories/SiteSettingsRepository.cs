using MongoDB.Driver;
using Nezabuti.Api.Models;

namespace Nezabuti.Api.Repositories;

public interface ISiteSettingsRepository
{
    Task<SiteSettings> GetAsync(CancellationToken ct = default);
    Task<SiteSettings> UpsertAsync(SiteSettings settings, CancellationToken ct = default);
}

public sealed class SiteSettingsRepository : ISiteSettingsRepository
{
    private readonly IMongoContext _db;

    public SiteSettingsRepository(IMongoContext db)
    {
        _db = db;
    }

    public async Task<SiteSettings> GetAsync(CancellationToken ct = default)
    {
        var existing = await _db.SiteSettings
            .Find(s => s.Id == SiteSettings.SingletonId)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            return existing;
        }

        var created = new SiteSettings
        {
            Id = SiteSettings.SingletonId,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _db.SiteSettings.InsertOneAsync(created, cancellationToken: ct);
        }
        catch (MongoWriteException)
        {
            existing = await _db.SiteSettings
                .Find(s => s.Id == SiteSettings.SingletonId)
                .FirstOrDefaultAsync(ct);
            if (existing is not null)
            {
                return existing;
            }

            throw;
        }

        return created;
    }

    public async Task<SiteSettings> UpsertAsync(SiteSettings settings, CancellationToken ct = default)
    {
        settings.Id = SiteSettings.SingletonId;
        settings.UpdatedAt = DateTime.UtcNow;

        var update = Builders<SiteSettings>.Update
            .Set(s => s.Phone, settings.Phone)
            .Set(s => s.Telegram, settings.Telegram)
            .Set(s => s.Viber, settings.Viber)
            .Set(s => s.AdditionalUpdatePrice, settings.AdditionalUpdatePrice)
            .Set(s => s.QrSize50PriceDelta, settings.QrSize50PriceDelta)
            .Set(s => s.QrSize75PriceDelta, settings.QrSize75PriceDelta)
            .Set(s => s.QrSize100PriceDelta, settings.QrSize100PriceDelta)
            .Set(s => s.ShortTextMaxChars, settings.ShortTextMaxChars)
            .Set(s => s.TextBlockMaxChars, settings.TextBlockMaxChars)
            .Set(s => s.QuoteMaxChars, settings.QuoteMaxChars)
            .Set(s => s.TimelineDescriptionMaxChars, settings.TimelineDescriptionMaxChars)
            .Set(s => s.MemoryTextMaxChars, settings.MemoryTextMaxChars)
            .Set(s => s.ServiceDescriptionMaxChars, settings.ServiceDescriptionMaxChars)
            .Set(s => s.AwardDescriptionMaxChars, settings.AwardDescriptionMaxChars)
            .Set(s => s.PhotoCaptionMaxChars, settings.PhotoCaptionMaxChars)
            .Set(s => s.UpdatedAt, settings.UpdatedAt)
            .SetOnInsert(s => s.Id, SiteSettings.SingletonId);

        var filter = Builders<SiteSettings>.Filter.Eq(s => s.Id, SiteSettings.SingletonId);
        var options = new FindOneAndUpdateOptions<SiteSettings, SiteSettings>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        return await _db.SiteSettings.FindOneAndUpdateAsync(filter, update, options, ct);
    }
}
