using MongoDB.Driver;
using Nezabuti.Api.Models;

namespace Nezabuti.Api.Repositories;

public interface IPlanRepository
{
    Task<List<Plan>> ListAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<Plan?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Plan?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Plan?> UpdateAsync(Plan plan, CancellationToken ct = default);
    Task EnsureBootstrapAsync(CancellationToken ct = default);
}

public sealed class PlanRepository : IPlanRepository
{
    private readonly IMongoCollection<Plan> _plans;

    public PlanRepository(IMongoContext mongo)
    {
        _plans = mongo.Plans;
    }

    public async Task EnsureBootstrapAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var seeds = BuildSeeds(now);

        foreach (var seed in seeds)
        {
            var existing = await _plans.Find(p => p.Code == seed.Code).FirstOrDefaultAsync(ct);
            if (existing is not null)
            {
                continue;
            }

            await _plans.InsertOneAsync(seed, cancellationToken: ct);
        }
    }

    public Task<List<Plan>> ListAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var filter = activeOnly
            ? Builders<Plan>.Filter.Eq(p => p.IsActive, true)
            : Builders<Plan>.Filter.Empty;
        return _plans.Find(filter).SortBy(p => p.Price).ThenBy(p => p.Name).ToListAsync(ct);
    }

    public Task<Plan?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _plans.Find(p => p.Id == id).FirstOrDefaultAsync(ct)!;

    public Task<Plan?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _plans.Find(p => p.Code == code).FirstOrDefaultAsync(ct)!;

    public async Task<Plan?> UpdateAsync(Plan plan, CancellationToken ct = default)
    {
        plan.UpdatedAt = DateTime.UtcNow;
        var result = await _plans.ReplaceOneAsync(p => p.Id == plan.Id, plan, cancellationToken: ct);
        return result.MatchedCount == 0 ? null : plan;
    }

    private static List<Plan> BuildSeeds(DateTime now) =>
    [
        new Plan
        {
            Code = PlanCodes.Memory,
            Name = "Пам’ять",
            Description = "Лаконічна сторінка пам’яті з основною історією та фотографіями.",
            Price = 900m,
            IsActive = true,
            IsCustom = false,
            IsUnlimited = false,
            MaxBlocks = 4,
            MaxGalleryBlocks = 1,
            MaxPhotosPerGallery = 10,
            MaxTimelineEvents = 5,
            MaxMemories = 2,
            IncludedUpdates = 1,
            CreatedAt = now,
            UpdatedAt = now
        },
        new Plan
        {
            Code = PlanCodes.Story,
            Name = "Історія",
            Description = "Розгорнута історія життя з кількома фотогалереями, життєвим шляхом і спогадами.",
            Price = 1400m,
            IsActive = true,
            IsCustom = false,
            IsUnlimited = false,
            MaxBlocks = 10,
            MaxGalleryBlocks = 2,
            MaxPhotosPerGallery = 20,
            MaxTimelineEvents = 15,
            MaxMemories = 8,
            IncludedUpdates = 2,
            CreatedAt = now,
            UpdatedAt = now
        },
        new Plan
        {
            Code = PlanCodes.Legacy,
            Name = "Спадщина",
            Description = "Повний цифровий меморіал для великої кількості фотографій, подій і спогадів.",
            Price = 2000m,
            IsActive = true,
            IsCustom = false,
            IsUnlimited = false,
            MaxBlocks = 20,
            MaxGalleryBlocks = 5,
            MaxPhotosPerGallery = 40,
            MaxTimelineEvents = 30,
            MaxMemories = 20,
            IncludedUpdates = 3,
            CreatedAt = now,
            UpdatedAt = now
        },
        new Plan
        {
            Code = PlanCodes.Custom,
            Name = "Custom",
            Description = "Індивідуальні умови для конкретного меморіалу.",
            Price = 0m,
            IsActive = true,
            IsCustom = true,
            IsUnlimited = true,
            MaxBlocks = null,
            MaxGalleryBlocks = null,
            MaxPhotosPerGallery = null,
            MaxTimelineEvents = null,
            MaxMemories = null,
            IncludedUpdates = 0,
            CreatedAt = now,
            UpdatedAt = now
        }
    ];
}
