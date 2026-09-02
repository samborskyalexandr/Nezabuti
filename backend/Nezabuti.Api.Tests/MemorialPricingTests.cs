using MongoDB.Bson;
using Nezabuti.Api.Models;
using Nezabuti.Api.Models.Blocks;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Tests;

public class MemorialPricingTests
{
    [Fact]
    public void CalculatePrice_StoryPlusQr75()
    {
        var calculated = MemorialPricing.CalculatePrice(1400m, 100m);
        Assert.Equal(1500m, calculated);
    }

    [Fact]
    public void ResolveFinalPrice_UsesOverrideWhenSet()
    {
        var memorial = new Memorial
        {
            PlanSnapshot = new PlanSnapshot { Price = 1400m },
            QrPriceDeltaSnapshot = 100m,
            FinalPrice = 1300m,
            IsFinalPriceOverridden = true
        };

        Assert.Equal(1300m, MemorialPricing.ResolveFinalPrice(memorial));
        Assert.Equal(1500m, MemorialPricing.ResolveCalculatedPrice(memorial));
    }

    [Fact]
    public void ResolveFinalPrice_FallsBackToCalculated_WhenNull()
    {
        var memorial = new Memorial
        {
            PlanSnapshot = new PlanSnapshot { Price = 1400m },
            QrPriceDeltaSnapshot = 100m,
            FinalPrice = null
        };

        Assert.Equal(1500m, MemorialPricing.ResolveFinalPrice(memorial));
    }

    [Fact]
    public void ResolveFinalPrice_Null_WhenNoPlan()
    {
        var memorial = new Memorial { PlanSnapshot = null, FinalPrice = null };
        Assert.Null(MemorialPricing.ResolveFinalPrice(memorial));
        Assert.Null(MemorialPricing.ResolveCalculatedPrice(memorial));
    }

    [Fact]
    public void GetQrPriceDelta_UsesSettingsPerSize()
    {
        var settings = new SiteSettings
        {
            QrSize50PriceDelta = 0,
            QrSize75PriceDelta = 100,
            QrSize100PriceDelta = 200
        };

        Assert.Equal(0, MemorialPricing.GetQrPriceDelta(settings, QrPlateSize.Size50));
        Assert.Equal(100, MemorialPricing.GetQrPriceDelta(settings, QrPlateSize.Size75));
        Assert.Equal(200, MemorialPricing.GetQrPriceDelta(settings, QrPlateSize.Size100));
    }

    [Fact]
    public void QrDeltaSnapshot_NotAffectedByLaterSettingsChange()
    {
        var memorial = new Memorial
        {
            PlanSnapshot = new PlanSnapshot { Price = 1400m },
            QrPlateSize = QrPlateSize.Size75,
            QrPriceDeltaSnapshot = 100m,
            FinalPrice = 1500m,
            IsFinalPriceOverridden = false
        };

        var newSettings = new SiteSettings { QrSize75PriceDelta = 150m };
        // Snapshot remains authoritative for calculated price of this memorial.
        Assert.Equal(1500m, MemorialPricing.ResolveCalculatedPrice(memorial));
        Assert.Equal(150m, MemorialPricing.GetQrPriceDelta(newSettings, QrPlateSize.Size75));
        Assert.NotEqual(
            MemorialPricing.ResolveCalculatedPrice(memorial),
            MemorialPricing.CalculatePrice(1400m, MemorialPricing.GetQrPriceDelta(newSettings, QrPlateSize.Size75)));
    }

    [Fact]
    public void PublicMemorialDto_HasNoPaymentFields()
    {
        var names = typeof(Nezabuti.Api.DTOs.PublicMemorialDto)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("IsDemo", names);
        Assert.DoesNotContain("PaymentStatus", names);
        Assert.DoesNotContain("FinalPrice", names);
        Assert.DoesNotContain("PaidAt", names);
        Assert.DoesNotContain("CalculatedPrice", names);
        Assert.DoesNotContain("QrPriceDeltaSnapshot", names);
    }
}

public class PlanUsageLiveRecalcTests
{
    private readonly PlanLimitService _sut = new();

    [Fact]
    public void GetUsage_AfterDeleteBlock_DecrementsBlocksUsed()
    {
        var snap = new PlanSnapshot
        {
            Name = "Історія",
            MaxBlocks = 4,
            MaxGalleryBlocks = 2,
            MaxPhotosPerGallery = 20,
            MaxTimelineEvents = 10,
            MaxMemories = 5,
            IncludedUpdates = 1
        };

        var memorial = new Memorial
        {
            PlanSnapshot = snap,
            Blocks =
            [
                TextBlock(),
                TextBlock(),
                TextBlock(),
                TextBlock()
            ]
        };

        Assert.Equal(4, _sut.GetUsage(memorial).BlocksUsed);
        Assert.False(_sut.CanAddBlock(memorial, "Quote").Ok);

        memorial.Blocks.RemoveAt(0);

        var usage = _sut.GetUsage(memorial);
        Assert.Equal(3, usage.BlocksUsed);
        Assert.True(_sut.CanAddBlock(memorial, "Quote").Ok);
    }

    [Fact]
    public void GetUsage_AfterDeleteGallery_AllowsNewGallery()
    {
        var snap = new PlanSnapshot
        {
            Name = "Історія",
            MaxBlocks = 10,
            MaxGalleryBlocks = 2,
            MaxPhotosPerGallery = 20
        };

        var memorial = new Memorial
        {
            PlanSnapshot = snap,
            Blocks =
            [
                GalleryBlock("g1", 1),
                GalleryBlock("g2", 1)
            ]
        };

        Assert.Equal(2, _sut.GetUsage(memorial).GalleriesUsed);
        Assert.False(_sut.CanAddBlock(memorial, "Gallery").Ok);

        memorial.Blocks.RemoveAt(0);

        Assert.Equal(1, _sut.GetUsage(memorial).GalleriesUsed);
        Assert.True(_sut.CanAddBlock(memorial, "Gallery").Ok);
    }

    [Fact]
    public void CanAddGalleryPhoto_AfterDelete_AllowsAgain()
    {
        var snap = new PlanSnapshot
        {
            Name = "Історія",
            MaxBlocks = 10,
            MaxGalleryBlocks = 2,
            MaxPhotosPerGallery = 2
        };

        var memorial = new Memorial
        {
            PlanSnapshot = snap,
            Blocks = [GalleryBlock("g1", 2)]
        };

        Assert.False(_sut.CanAddGalleryPhoto(memorial, "g1").Ok);

        var gallery = memorial.Blocks[0];
        var items = gallery.Data["items"].AsBsonArray;
        items.RemoveAt(0);

        Assert.True(_sut.CanAddGalleryPhoto(memorial, "g1").Ok);
    }

    private static MemorialBlock TextBlock() => new()
    {
        Id = ObjectId.GenerateNewId().ToString(),
        Type = BlockType.Text,
        Order = 0,
        Data = new MongoDB.Bson.BsonDocument { ["html"] = "<p>x</p>" }
    };

    private static MemorialBlock GalleryBlock(string id, int photoCount)
    {
        var items = new MongoDB.Bson.BsonArray();
        for (var i = 0; i < photoCount; i++)
        {
            items.Add(new MongoDB.Bson.BsonDocument { ["photoId"] = $"p{i}", ["caption"] = "" });
        }

        return new MemorialBlock
        {
            Id = id,
            Type = BlockType.Gallery,
            Order = 0,
            Data = new MongoDB.Bson.BsonDocument { ["items"] = items }
        };
    }
}
