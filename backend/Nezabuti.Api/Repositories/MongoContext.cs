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
    IMongoCollection<Plan> Plans { get; }
    IMongoCollection<Customer> Customers { get; }
    IMongoCollection<MemorialPayment> MemorialPayments { get; }
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
        Plans = _db.GetCollection<Plan>("plans");
        Customers = _db.GetCollection<Customer>("customers");
        MemorialPayments = _db.GetCollection<MemorialPayment>("memorial_payments");
    }

    public IMongoCollection<Memorial> Memorials { get; }
    public IMongoCollection<MemorialStatistics> Statistics { get; }
    public IMongoCollection<SiteSettings> SiteSettings { get; }
    public IMongoCollection<Plan> Plans { get; }
    public IMongoCollection<Customer> Customers { get; }
    public IMongoCollection<MemorialPayment> MemorialPayments { get; }

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

        var memorialCustomerIndex = new CreateIndexModel<Memorial>(
            Builders<Memorial>.IndexKeys.Ascending(m => m.CustomerId),
            new CreateIndexOptions { Name = "ix_memorial_customerId" });

        await Memorials.Indexes.CreateManyAsync(
            [publicIdIndex, statusIndex, statusUpdatedIndex, fullNameIndex, memorialCustomerIndex],
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

        var planCodeIndex = new CreateIndexModel<Plan>(
            Builders<Plan>.IndexKeys.Ascending(p => p.Code),
            new CreateIndexOptions { Unique = true, Name = "ux_plan_code" });

        await Plans.Indexes.CreateOneAsync(planCodeIndex, cancellationToken: cancellationToken);

        var customerPhoneIndex = new CreateIndexModel<Customer>(
            Builders<Customer>.IndexKeys.Ascending(c => c.Phone),
            new CreateIndexOptions { Name = "ix_customer_phone" });

        await Customers.Indexes.CreateOneAsync(customerPhoneIndex, cancellationToken: cancellationToken);

        var paymentMemorialIndex = new CreateIndexModel<MemorialPayment>(
            Builders<MemorialPayment>.IndexKeys.Ascending(p => p.MemorialId),
            new CreateIndexOptions { Name = "ix_payment_memorialId" });

        await MemorialPayments.Indexes.CreateOneAsync(paymentMemorialIndex, cancellationToken: cancellationToken);
    }
}
