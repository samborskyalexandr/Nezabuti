using MongoDB.Driver;
using Nezabuti.Api.Models;

namespace Nezabuti.Api.Repositories;

public interface ISiteSettingsRepository
{
    Task<SiteSettings> GetAsync(CancellationToken ct = default);
    Task<SiteSettings> UpsertAsync(string phone, string telegram, string viber, CancellationToken ct = default);
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
            // Concurrent first write — re-read
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

    public async Task<SiteSettings> UpsertAsync(string phone, string telegram, string viber, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var update = Builders<SiteSettings>.Update
            .Set(s => s.Phone, phone)
            .Set(s => s.Telegram, telegram)
            .Set(s => s.Viber, viber)
            .Set(s => s.UpdatedAt, now)
            .SetOnInsert(s => s.Id, SiteSettings.SingletonId);

        var filter = Builders<SiteSettings>.Filter.Eq(s => s.Id, SiteSettings.SingletonId);
        var options = new FindOneAndUpdateOptions<SiteSettings, SiteSettings>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        return await _db.SiteSettings.FindOneAndUpdateAsync(
            filter,
            update,
            options,
            ct);
    }
}
