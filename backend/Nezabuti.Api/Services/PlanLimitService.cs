using MongoDB.Bson;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Models.Blocks;

namespace Nezabuti.Api.Services;

public sealed class PlanUsageDto
{
    public int BlocksUsed { get; set; }
    public int? MaxBlocks { get; set; }
    public int GalleriesUsed { get; set; }
    public int? MaxGalleryBlocks { get; set; }
    public int TimelineEventsUsed { get; set; }
    public int? MaxTimelineEvents { get; set; }
    public int MemoriesUsed { get; set; }
    public int? MaxMemories { get; set; }
    public int UsedUpdates { get; set; }
    public int IncludedUpdates { get; set; }
    public bool IsUnlimited { get; set; }
    public List<GalleryUsageDto> Galleries { get; set; } = [];
}

public sealed class GalleryUsageDto
{
    public string BlockId { get; set; } = string.Empty;
    public int PhotosUsed { get; set; }
    public int? MaxPhotosPerGallery { get; set; }
}

public interface IPlanLimitService
{
    PlanUsageDto GetUsage(Memorial memorial);
    (bool Ok, string? Error) CanAddBlock(Memorial memorial, string blockType);
    (bool Ok, string? Error) CanAddGalleryPhoto(Memorial memorial, string galleryBlockId, int photosAfterAdd = 1);
    (bool Ok, string? Error) CanAddTimelineEvent(Memorial memorial, string timelineBlockId);
    (bool Ok, string? Error) CanAddMemory(Memorial memorial, string memoriesBlockId);
    (bool Ok, string? Error) ValidateMemorialAgainstPlan(Memorial memorial, PlanSnapshot snapshot);
    PlanSnapshot CreateSnapshot(Plan plan, PlanSnapshot? customOverrides = null);
    void ApplyCustomOverrides(PlanSnapshot snapshot, CustomPlanOverridesDto overrides);
}

public sealed class PlanLimitService : IPlanLimitService
{
    public PlanSnapshot CreateSnapshot(Plan plan, PlanSnapshot? customOverrides = null)
    {
        var snap = new PlanSnapshot
        {
            PlanId = plan.Id,
            Code = plan.Code,
            Name = plan.Name,
            Price = customOverrides?.Price ?? plan.Price,
            IsCustom = plan.IsCustom,
            IsUnlimited = customOverrides?.IsUnlimited ?? plan.IsUnlimited,
            MaxBlocks = customOverrides?.MaxBlocks ?? plan.MaxBlocks,
            MaxGalleryBlocks = customOverrides?.MaxGalleryBlocks ?? plan.MaxGalleryBlocks,
            MaxPhotosPerGallery = customOverrides?.MaxPhotosPerGallery ?? plan.MaxPhotosPerGallery,
            MaxTimelineEvents = customOverrides?.MaxTimelineEvents ?? plan.MaxTimelineEvents,
            MaxMemories = customOverrides?.MaxMemories ?? plan.MaxMemories,
            IncludedUpdates = customOverrides?.IncludedUpdates ?? plan.IncludedUpdates,
            SnapshotAt = DateTime.UtcNow
        };

        if (snap.IsUnlimited)
        {
            snap.MaxBlocks = null;
            snap.MaxGalleryBlocks = null;
            snap.MaxPhotosPerGallery = null;
            snap.MaxTimelineEvents = null;
            snap.MaxMemories = null;
        }

        return snap;
    }

    public void ApplyCustomOverrides(PlanSnapshot snapshot, CustomPlanOverridesDto overrides)
    {
        if (overrides.Price.HasValue)
        {
            snapshot.Price = overrides.Price.Value;
        }

        if (overrides.IsUnlimited.HasValue)
        {
            snapshot.IsUnlimited = overrides.IsUnlimited.Value;
        }

        if (overrides.MaxBlocks.HasValue)
        {
            snapshot.MaxBlocks = overrides.MaxBlocks;
        }

        if (overrides.MaxGalleryBlocks.HasValue)
        {
            snapshot.MaxGalleryBlocks = overrides.MaxGalleryBlocks;
        }

        if (overrides.MaxPhotosPerGallery.HasValue)
        {
            snapshot.MaxPhotosPerGallery = overrides.MaxPhotosPerGallery;
        }

        if (overrides.MaxTimelineEvents.HasValue)
        {
            snapshot.MaxTimelineEvents = overrides.MaxTimelineEvents;
        }

        if (overrides.MaxMemories.HasValue)
        {
            snapshot.MaxMemories = overrides.MaxMemories;
        }

        if (overrides.IncludedUpdates.HasValue)
        {
            snapshot.IncludedUpdates = Math.Max(0, overrides.IncludedUpdates.Value);
        }

        if (snapshot.IsUnlimited)
        {
            snapshot.MaxBlocks = null;
            snapshot.MaxGalleryBlocks = null;
            snapshot.MaxPhotosPerGallery = null;
            snapshot.MaxTimelineEvents = null;
            snapshot.MaxMemories = null;
        }
    }

    public PlanUsageDto GetUsage(Memorial memorial)
    {
        var snap = memorial.PlanSnapshot;
        var unlimited = snap?.IsUnlimited == true;
        var galleries = GetGalleryBlocks(memorial.Blocks);

        return new PlanUsageDto
        {
            BlocksUsed = memorial.Blocks.Count,
            MaxBlocks = unlimited ? null : snap?.MaxBlocks,
            GalleriesUsed = galleries.Count,
            MaxGalleryBlocks = unlimited ? null : snap?.MaxGalleryBlocks,
            TimelineEventsUsed = CountTimelineEvents(memorial.Blocks),
            MaxTimelineEvents = unlimited ? null : snap?.MaxTimelineEvents,
            MemoriesUsed = CountMemories(memorial.Blocks),
            MaxMemories = unlimited ? null : snap?.MaxMemories,
            UsedUpdates = memorial.UsedUpdates,
            IncludedUpdates = snap?.IncludedUpdates ?? 0,
            IsUnlimited = unlimited,
            Galleries = galleries.Select(g => new GalleryUsageDto
            {
                BlockId = g.Id,
                PhotosUsed = CountGalleryPhotos(g),
                MaxPhotosPerGallery = unlimited ? null : snap?.MaxPhotosPerGallery
            }).ToList()
        };
    }

    public (bool Ok, string? Error) CanAddBlock(Memorial memorial, string blockType)
    {
        var snap = memorial.PlanSnapshot;
        if (snap is null)
        {
            return (false, "Спочатку призначте план для цього меморіалу.");
        }

        if (snap.IsUnlimited)
        {
            return (true, null);
        }

        if (snap.MaxBlocks is int maxBlocks && memorial.Blocks.Count >= maxBlocks)
        {
            return (false, $"У плані «{snap.Name}» доступно до {maxBlocks} блоків.");
        }

        if (string.Equals(blockType, BlockType.Gallery, StringComparison.OrdinalIgnoreCase)
            && snap.MaxGalleryBlocks is int maxGalleries
            && GetGalleryBlocks(memorial.Blocks).Count >= maxGalleries)
        {
            return (false, $"У плані «{snap.Name}» доступно до {maxGalleries} галерей.");
        }

        return (true, null);
    }

    public (bool Ok, string? Error) CanAddGalleryPhoto(Memorial memorial, string galleryBlockId, int photosAfterAdd = 1)
    {
        var snap = memorial.PlanSnapshot;
        if (snap is null)
        {
            return (false, "Спочатку призначте план для цього меморіалу.");
        }

        if (snap.IsUnlimited || snap.MaxPhotosPerGallery is null)
        {
            return (true, null);
        }

        var gallery = memorial.Blocks.FirstOrDefault(b =>
            b.Id == galleryBlockId
            && string.Equals(b.Type, BlockType.Gallery, StringComparison.OrdinalIgnoreCase));

        if (gallery is null)
        {
            return (false, "Галерею не знайдено.");
        }

        var used = CountGalleryPhotos(gallery);
        var max = snap.MaxPhotosPerGallery.Value;
        if (used + photosAfterAdd > max)
        {
            return (false, $"У цій галереї можна додати до {max} фото.");
        }

        return (true, null);
    }

    public (bool Ok, string? Error) CanAddTimelineEvent(Memorial memorial, string timelineBlockId)
    {
        var snap = memorial.PlanSnapshot;
        if (snap is null)
        {
            return (false, "Спочатку призначте план для цього меморіалу.");
        }

        if (snap.IsUnlimited || snap.MaxTimelineEvents is null)
        {
            return (true, null);
        }

        var used = CountTimelineEvents(memorial.Blocks);
        if (used >= snap.MaxTimelineEvents.Value)
        {
            return (false, $"У плані «{snap.Name}» доступно до {snap.MaxTimelineEvents} подій життєвого шляху.");
        }

        return (true, null);
    }

    public (bool Ok, string? Error) CanAddMemory(Memorial memorial, string memoriesBlockId)
    {
        var snap = memorial.PlanSnapshot;
        if (snap is null)
        {
            return (false, "Спочатку призначте план для цього меморіалу.");
        }

        if (snap.IsUnlimited || snap.MaxMemories is null)
        {
            return (true, null);
        }

        var used = CountMemories(memorial.Blocks);
        if (used >= snap.MaxMemories.Value)
        {
            return (false, $"У плані «{snap.Name}» доступно до {snap.MaxMemories} спогадів.");
        }

        return (true, null);
    }

    public (bool Ok, string? Error) ValidateMemorialAgainstPlan(Memorial memorial, PlanSnapshot snapshot)
    {
        if (snapshot.IsUnlimited)
        {
            return (true, null);
        }

        var name = snapshot.Name;

        if (snapshot.MaxBlocks is int maxBlocks && memorial.Blocks.Count > maxBlocks)
        {
            return (false, $"Поточний меморіал має {memorial.Blocks.Count} блоків, а план «{name}» дозволяє лише {maxBlocks}.");
        }

        var galleries = GetGalleryBlocks(memorial.Blocks);
        if (snapshot.MaxGalleryBlocks is int maxGalleries && galleries.Count > maxGalleries)
        {
            return (false, $"Поточний меморіал має {galleries.Count} галерей, а план «{name}» дозволяє лише {maxGalleries}.");
        }

        if (snapshot.MaxPhotosPerGallery is int maxPhotos)
        {
            foreach (var g in galleries)
            {
                var used = CountGalleryPhotos(g);
                if (used > maxPhotos)
                {
                    return (false, $"Одна з галерей містить {used} фото, а план «{name}» дозволяє до {maxPhotos} у кожній.");
                }
            }
        }

        var timeline = CountTimelineEvents(memorial.Blocks);
        if (snapshot.MaxTimelineEvents is int maxTimeline && timeline > maxTimeline)
        {
            return (false, $"Поточний меморіал має {timeline} подій життєвого шляху, а план «{name}» дозволяє лише {maxTimeline}.");
        }

        var memories = CountMemories(memorial.Blocks);
        if (snapshot.MaxMemories is int maxMemories && memories > maxMemories)
        {
            return (false, $"Поточний меморіал має {memories} спогадів, а план «{name}» дозволяє лише {maxMemories}.");
        }

        return (true, null);
    }

    private static List<MemorialBlock> GetGalleryBlocks(IEnumerable<MemorialBlock> blocks) =>
        blocks.Where(b => string.Equals(b.Type, BlockType.Gallery, StringComparison.OrdinalIgnoreCase)).ToList();

    private static int CountGalleryPhotos(MemorialBlock gallery)
    {
        if (!gallery.Data.TryGetValue("items", out var items) || items is not BsonArray arr)
        {
            return 0;
        }

        return arr.Count(x => x.IsBsonDocument && x.AsBsonDocument.Contains("photoId")
            && !string.IsNullOrWhiteSpace(x.AsBsonDocument.GetValue("photoId", "").AsString));
    }

    private static int CountTimelineEvents(IEnumerable<MemorialBlock> blocks)
    {
        var total = 0;
        foreach (var block in blocks.Where(b => string.Equals(b.Type, BlockType.Timeline, StringComparison.OrdinalIgnoreCase)))
        {
            if (block.Data.TryGetValue("events", out var events) && events is BsonArray arr)
            {
                total += arr.Count;
            }
        }

        return total;
    }

    private static int CountMemories(IEnumerable<MemorialBlock> blocks)
    {
        var total = 0;
        foreach (var block in blocks.Where(b => string.Equals(b.Type, BlockType.Memories, StringComparison.OrdinalIgnoreCase)))
        {
            if (block.Data.TryGetValue("items", out var items) && items is BsonArray arr)
            {
                total += arr.Count;
            }
        }

        return total;
    }
}
