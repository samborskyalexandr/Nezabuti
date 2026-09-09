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
                var changed = false;

                // One-time migration from previous seed list prices → new annual model defaults.
                // Only touches exact old defaults so admin-edited prices stay intact.
                if (!existing.IsCustom)
                {
                    var (oldInitial, newInitial) = existing.Code switch
                    {
                        PlanCodes.Memory => (900m, 700m),
                        PlanCodes.Story => (1400m, 1200m),
                        PlanCodes.Legacy => (2000m, 1900m),
                        _ => (0m, 0m)
                    };

                    if (oldInitial > 0)
                    {
                        var currentInitial = existing.ResolveInitialPrice();
                        if (currentInitial == oldInitial)
                        {
                            existing.InitialPrice = newInitial;
                            existing.Price = newInitial;
                            changed = true;
                        }
                    }

                    if (existing.RenewalPrice <= 0)
                    {
                        existing.RenewalPrice = seed.RenewalPrice;
                        changed = true;
                    }
                }
                else if (existing.InitialPrice <= 0 && existing.Price > 0)
                {
                    existing.InitialPrice = existing.Price;
                    changed = true;
                }
                else if (existing.InitialPrice <= 0)
                {
                    existing.InitialPrice = seed.InitialPrice;
                    existing.Price = seed.InitialPrice;
                    changed = true;
                }

                if (changed)
                {
                    existing.UpdatedAt = now;
                    await _plans.ReplaceOneAsync(p => p.Id == existing.Id, existing, cancellationToken: ct);
                }

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
        return _plans.Find(filter).SortBy(p => p.InitialPrice).ThenBy(p => p.Price).ThenBy(p => p.Name).ToListAsync(ct);
    }

    public Task<Plan?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _plans.Find(p => p.Id == id).FirstOrDefaultAsync(ct)!;

    public Task<Plan?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _plans.Find(p => p.Code == code).FirstOrDefaultAsync(ct)!;

    public async Task<Plan?> UpdateAsync(Plan plan, CancellationToken ct = default)
    {
        plan.UpdatedAt = DateTime.UtcNow;
        if (plan.InitialPrice <= 0 && plan.Price > 0)
        {
            plan.InitialPrice = plan.Price;
        }

        plan.Price = plan.ResolveInitialPrice();
        var result = await _plans.ReplaceOneAsync(p => p.Id == plan.Id, plan, cancellationToken: ct);
        return result.MatchedCount == 0 ? null : plan;
    }

    private static List<Plan> BuildSeeds(DateTime now) =>
    [
        new Plan
        {
            Code = PlanCodes.Memory,
            Name = "Пам’ять",
            Description = "Лаконічна сторінка пам’яті з основною історією та фотографіями. Перший платіж включає створення та перший рік розміщення.",
            Price = 700m,
            InitialPrice = 700m,
            RenewalPrice = 300m,
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
            Description = "Розгорнута історія життя з кількома фотогалереями, життєвим шляхом і спогадами. Перший платіж включає створення та перший рік розміщення.",
            Price = 1200m,
            InitialPrice = 1200m,
            RenewalPrice = 400m,
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
            Description = "Повний цифровий меморіал для великої кількості фотографій, подій і спогадів. Перший платіж включає створення та перший рік розміщення.",
            Price = 1900m,
            InitialPrice = 1900m,
            RenewalPrice = 500m,
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
            Description = "Індивідуальні умови для конкретного меморіалу. Ціни задає адміністратор.",
            Price = 0m,
            InitialPrice = 0m,
            RenewalPrice = 0m,
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
