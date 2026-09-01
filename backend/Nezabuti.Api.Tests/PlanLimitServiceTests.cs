using MongoDB.Bson;
using Nezabuti.Api.Models;
using Nezabuti.Api.Models.Blocks;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Tests;

public class PlanLimitServiceTests
{
    private readonly PlanLimitService _sut = new();

    [Fact]
    public void CreateSnapshot_UnlimitedCustom_ClearsLimits()
    {
        var plan = new Plan
        {
            Id = "aaaaaaaaaaaaaaaaaaaaaaaa",
            Code = PlanCodes.Custom,
            Name = "Custom",
            IsCustom = true,
            IsUnlimited = true,
            MaxBlocks = 5,
            MaxGalleryBlocks = 2,
            MaxPhotosPerGallery = 10,
            MaxTimelineEvents = 8,
            MaxMemories = 3,
            IncludedUpdates = 0,
            Price = 0
        };

        var snap = _sut.CreateSnapshot(plan);

        Assert.True(snap.IsUnlimited);
        Assert.Null(snap.MaxBlocks);
        Assert.Null(snap.MaxGalleryBlocks);
        Assert.Null(snap.MaxPhotosPerGallery);
        Assert.Null(snap.MaxTimelineEvents);
        Assert.Null(snap.MaxMemories);
    }

    [Fact]
    public void ValidateMemorialAgainstPlan_Unlimited_AlwaysOk()
    {
        var memorial = MemorialWithBlocks(6);
        var snap = LimitedSnapshot(maxBlocks: 2);
        snap.IsUnlimited = true;

        var (ok, error) = _sut.ValidateMemorialAgainstPlan(memorial, snap);

        Assert.True(ok);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateMemorialAgainstPlan_BlockOverage_Fails()
    {
        var memorial = MemorialWithBlocks(5);
        var snap = LimitedSnapshot(maxBlocks: 3);

        var (ok, error) = _sut.ValidateMemorialAgainstPlan(memorial, snap);

        Assert.False(ok);
        Assert.Contains("блоків", error!);
    }

    [Fact]
    public void ValidateMemorialAgainstPlan_GalleryOverage_Fails()
    {
        var memorial = new Memorial
        {
            Blocks =
            [
                GalleryBlock("g1", photoCount: 1),
                GalleryBlock("g2", photoCount: 1)
            ],
            PlanSnapshot = LimitedSnapshot(maxGalleryBlocks: 1)
        };
        var snap = LimitedSnapshot(maxBlocks: 10, maxGalleryBlocks: 1);

        var (ok, error) = _sut.ValidateMemorialAgainstPlan(memorial, snap);

        Assert.False(ok);
        Assert.Contains("галерей", error!);
    }

    [Fact]
    public void ValidateMemorialAgainstPlan_PhotoOverage_Fails()
    {
        var memorial = new Memorial
        {
            Blocks = [GalleryBlock("g1", photoCount: 5)]
        };
        var snap = LimitedSnapshot(maxBlocks: 10, maxGalleryBlocks: 2, maxPhotos: 3);

        var (ok, error) = _sut.ValidateMemorialAgainstPlan(memorial, snap);

        Assert.False(ok);
        Assert.Contains("фото", error!);
    }

    [Fact]
    public void ValidateMemorialAgainstPlan_TimelineOverage_Fails()
    {
        var memorial = new Memorial
        {
            Blocks =
            [
                new MemorialBlock
                {
                    Id = "t1",
                    Type = BlockType.Timeline,
                    Order = 0,
                    Data = new BsonDocument
                    {
                        {
                            "events",
                            new BsonArray
                            {
                                new BsonDocument { { "title", "A" } },
                                new BsonDocument { { "title", "B" } },
                                new BsonDocument { { "title", "C" } }
                            }
                        }
                    }
                }
            ]
        };
        var snap = LimitedSnapshot(maxBlocks: 10, maxTimeline: 2);

        var (ok, error) = _sut.ValidateMemorialAgainstPlan(memorial, snap);

        Assert.False(ok);
        Assert.Contains("життєвого шляху", error!);
    }

    [Fact]
    public void ValidateMemorialAgainstPlan_MemoryOverage_Fails()
    {
        var memorial = new Memorial
        {
            Blocks =
            [
                new MemorialBlock
                {
                    Id = "m1",
                    Type = BlockType.Memories,
                    Order = 0,
                    Data = new BsonDocument
                    {
                        {
                            "items",
                            new BsonArray
                            {
                                new BsonDocument { { "text", "one" } },
                                new BsonDocument { { "text", "two" } },
                                new BsonDocument { { "text", "three" } }
                            }
                        }
                    }
                }
            ]
        };
        var snap = LimitedSnapshot(maxBlocks: 10, maxMemories: 2);

        var (ok, error) = _sut.ValidateMemorialAgainstPlan(memorial, snap);

        Assert.False(ok);
        Assert.Contains("спогадів", error!);
    }

    private static Memorial MemorialWithBlocks(int count)
    {
        var blocks = Enumerable.Range(0, count)
            .Select(i => new MemorialBlock
            {
                Id = $"b{i}",
                Type = BlockType.Text,
                Order = i,
                Data = new BsonDocument { { "html", "<p>x</p>" } }
            })
            .ToList();
        return new Memorial { Blocks = blocks };
    }

    private static MemorialBlock GalleryBlock(string id, int photoCount)
    {
        var items = new BsonArray();
        for (var i = 0; i < photoCount; i++)
        {
            items.Add(new BsonDocument { { "photoId", $"p{i}" } });
        }

        return new MemorialBlock
        {
            Id = id,
            Type = BlockType.Gallery,
            Order = 0,
            Data = new BsonDocument { { "items", items } }
        };
    }

    private static PlanSnapshot LimitedSnapshot(
        int maxBlocks = 4,
        int? maxGalleryBlocks = null,
        int? maxPhotos = null,
        int? maxTimeline = null,
        int? maxMemories = null) => new()
    {
        PlanId = "bbbbbbbbbbbbbbbbbbbbbbbb",
        Code = PlanCodes.Memory,
        Name = "Пам’ять",
        Price = 900,
        IsUnlimited = false,
        MaxBlocks = maxBlocks,
        MaxGalleryBlocks = maxGalleryBlocks,
        MaxPhotosPerGallery = maxPhotos,
        MaxTimelineEvents = maxTimeline,
        MaxMemories = maxMemories,
        IncludedUpdates = 1,
        SnapshotAt = DateTime.UtcNow
    };
}
