using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Nezabuti.Api.Configuration;
using Nezabuti.Api.Models;

namespace Nezabuti.Api.Repositories;

public interface IMongoContext
{
    IMongoCollection<Memorial> Memorials { get; }
    IMongoCollection<MemorialStatistics> Statistics { get; }
    IMongoCollection<SiteSettings> SiteSettings { get; }
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}

public sealed class MongoContext : IMongoContext
{
    private readonly IMongoDatabase _db;

    public MongoContext(IOptions<MongoSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        _db = client.GetDatabase(settings.DatabaseName);
        Memorials = _db.GetCollection<Memorial>("memorials");
        Statistics = _db.GetCollection<MemorialStatistics>("memorial_statistics");
        SiteSettings = _db.GetCollection<SiteSettings>("site_settings");
    }

    public IMongoCollection<Memorial> Memorials { get; }
    public IMongoCollection<MemorialStatistics> Statistics { get; }
    public IMongoCollection<SiteSettings> SiteSettings { get; }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var publicIdIndex = new CreateIndexModel<Memorial>(
            Builders<Memorial>.IndexKeys.Ascending(m => m.PublicId),
            new CreateIndexOptions { Unique = true, Name = "ux_publicId" });

        var statusIndex = new CreateIndexModel<Memorial>(
            Builders<Memorial>.IndexKeys.Ascending(m => m.Status),
            new CreateIndexOptions { Name = "ix_status" });

        var statusUpdatedIndex = new CreateIndexModel<Memorial>(
            Builders<Memorial>.IndexKeys
                .Ascending(m => m.Status)
                .Descending(m => m.UpdatedAt),
            new CreateIndexOptions { Name = "ix_status_updatedAt" });

        var fullNameIndex = new CreateIndexModel<Memorial>(
            Builders<Memorial>.IndexKeys.Ascending(m => m.FullName),
            new CreateIndexOptions { Name = "ix_fullName" });

        await Memorials.Indexes.CreateManyAsync(
            [publicIdIndex, statusIndex, statusUpdatedIndex, fullNameIndex],
            cancellationToken);

        var statsMemorialIndex = new CreateIndexModel<MemorialStatistics>(
            Builders<MemorialStatistics>.IndexKeys.Ascending(s => s.MemorialId),
            new CreateIndexOptions { Unique = true, Name = "ux_memorialId" });

        var statsPublicIdIndex = new CreateIndexModel<MemorialStatistics>(
            Builders<MemorialStatistics>.IndexKeys.Ascending(s => s.PublicId),
            new CreateIndexOptions { Unique = true, Name = "ux_stats_publicId" });

        await Statistics.Indexes.CreateManyAsync(
            [statsMemorialIndex, statsPublicIdIndex],
            cancellationToken);
    }
}
