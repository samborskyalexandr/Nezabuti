using MongoDB.Driver;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Repositories;

namespace Nezabuti.Api.Services;

public interface IStatisticsService
{
    Task EnsureExistsAsync(Memorial memorial, CancellationToken ct = default);
    Task RecordPublicViewAsync(string publicId, bool isAdminPreview, CancellationToken ct = default);
    Task<MemorialStatisticsDto?> GetAsync(string publicId, CancellationToken ct = default);
    Task DeleteByMemorialIdAsync(string memorialId, CancellationToken ct = default);
}

public sealed class StatisticsService : IStatisticsService
{
    private readonly IMongoContext _db;

    public StatisticsService(IMongoContext db)
    {
        _db = db;
    }

    public async Task EnsureExistsAsync(Memorial memorial, CancellationToken ct = default)
    {
        var existing = await _db.Statistics
            .Find(s => s.MemorialId == memorial.Id)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            return;
        }

        await _db.Statistics.InsertOneAsync(new MemorialStatistics
        {
            MemorialId = memorial.Id,
            PublicId = memorial.PublicId,
            TotalViews = 0,
            ViewsPerDay = []
        }, cancellationToken: ct);
    }

    public async Task RecordPublicViewAsync(string publicId, bool isAdminPreview, CancellationToken ct = default)
    {
        // Demo memorials are counted the same as ordinary public visits.
        // Only admin preview is excluded from statistics.
        if (isAdminPreview)
        {
            return;
        }

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var filter = Builders<MemorialStatistics>.Filter.Eq(s => s.PublicId, publicId);
        var stats = await _db.Statistics.Find(filter).FirstOrDefaultAsync(ct);
        if (stats is null)
        {
            return;
        }

        var day = stats.ViewsPerDay.FirstOrDefault(d => d.Date == today);
        if (day is null)
        {
            stats.ViewsPerDay.Add(new DailyViewCount { Date = today, Count = 1 });
        }
        else
        {
            day.Count += 1;
        }

        // Keep a bounded window (~365 days)
        if (stats.ViewsPerDay.Count > 400)
        {
            stats.ViewsPerDay = stats.ViewsPerDay
                .OrderByDescending(d => d.Date)
                .Take(365)
                .OrderBy(d => d.Date)
                .ToList();
        }

        stats.TotalViews += 1;
        stats.LastViewedAt = DateTime.UtcNow;

        await _db.Statistics.ReplaceOneAsync(filter, stats, cancellationToken: ct);
    }

    public async Task<MemorialStatisticsDto?> GetAsync(string publicId, CancellationToken ct = default)
    {
        var stats = await _db.Statistics.Find(s => s.PublicId == publicId).FirstOrDefaultAsync(ct);
        if (stats is null)
        {
            return null;
        }

        return new MemorialStatisticsDto
        {
            PublicId = stats.PublicId,
            TotalViews = stats.TotalViews,
            LastViewedAt = stats.LastViewedAt,
            ViewsPerDay = stats.ViewsPerDay
                .OrderByDescending(d => d.Date)
                .Select(d => new DailyViewCountDto { Date = d.Date, Count = d.Count })
                .ToList()
        };
    }

    public async Task DeleteByMemorialIdAsync(string memorialId, CancellationToken ct = default)
    {
        await _db.Statistics.DeleteOneAsync(s => s.MemorialId == memorialId, ct);
    }
}
