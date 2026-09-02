using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Tests;

public class MemorialDemoTests
{
    [Fact]
    public void Memorial_IsDemo_DefaultsToFalse()
    {
        Assert.False(new Memorial().IsDemo);
        Assert.False(new CreateMemorialRequest().IsDemo);
        Assert.False(new UpdateMemorialRequest().IsDemo);
        Assert.False(new MemorialListItemDto().IsDemo);
        Assert.False(new MemorialAdminDto().IsDemo);
        Assert.False(new PublicMemorialDto().IsDemo);
    }

    [Fact]
    public void Memorial_MissingIsDemoField_DeserializesAsFalse()
    {
        var doc = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "publicId", "abc123xy" },
            { "fullName", "Тест" }
        };

        var memorial = BsonSerializer.Deserialize<Memorial>(doc);

        Assert.False(memorial.IsDemo);
    }

    [Fact]
    public void Memorial_IsDemoTrue_RoundTripsThroughBson()
    {
        var memorial = new Memorial
        {
            Id = ObjectId.GenerateNewId().ToString(),
            PublicId = "demo0001",
            FullName = "Демо",
            IsDemo = true
        };

        var doc = memorial.ToBsonDocument();
        Assert.True(doc["isDemo"].AsBoolean);

        var restored = BsonSerializer.Deserialize<Memorial>(doc);
        Assert.True(restored.IsDemo);
    }

    [Fact]
    public void Seo_DemoPublic_IsNoIndexNofollow()
    {
        Assert.Equal(
            MemorialSeo.NoIndexNofollow,
            MemorialSeo.Robots(isDemo: true, MemorialPrivacy.Public));
    }

    [Fact]
    public void Seo_NormalPublic_IsIndexFollow()
    {
        Assert.Equal(
            MemorialSeo.IndexFollow,
            MemorialSeo.Robots(isDemo: false, MemorialPrivacy.Public));
    }

    [Fact]
    public void Seo_NormalPrivate_IsNoIndexNofollow()
    {
        Assert.Equal(
            MemorialSeo.NoIndexNofollow,
            MemorialSeo.Robots(isDemo: false, MemorialPrivacy.Private));
    }

    [Fact]
    public void Seo_AdminPreview_IsNoIndexNofollow_EvenForPublic()
    {
        Assert.Equal(
            MemorialSeo.NoIndexNofollow,
            MemorialSeo.Robots(isDemo: false, MemorialPrivacy.Public, forAdminPreview: true));
    }

    [Fact]
    public void PublicMemorialDto_ExposesIsDemo_WithoutAdminFields()
    {
        var names = typeof(PublicMemorialDto)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("IsDemo", names);
        Assert.DoesNotContain("PaymentStatus", names);
        Assert.DoesNotContain("FinalPrice", names);
        Assert.DoesNotContain("PaidAt", names);
        Assert.DoesNotContain("CalculatedPrice", names);
        Assert.DoesNotContain("IsFinalPriceOverridden", names);
        Assert.DoesNotContain("QrPriceDeltaSnapshot", names);
        Assert.DoesNotContain("UsedUpdates", names);
        Assert.DoesNotContain("PlanSnapshot", names);
    }

    [Fact]
    public void AdminDtos_ExposeIsDemo()
    {
        Assert.Contains("IsDemo", typeof(CreateMemorialRequest).GetProperties().Select(p => p.Name));
        Assert.Contains("IsDemo", typeof(UpdateMemorialRequest).GetProperties().Select(p => p.Name));
        Assert.Contains("IsDemo", typeof(MemorialAdminDto).GetProperties().Select(p => p.Name));
        Assert.Contains("IsDemo", typeof(MemorialListItemDto).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void Statistics_RecordPublicView_DoesNotTakeIsDemo()
    {
        var names = typeof(IStatisticsService)
            .GetMethod(nameof(IStatisticsService.RecordPublicViewAsync))!
            .GetParameters()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("publicId", names);
        Assert.Contains("isAdminPreview", names);
        Assert.DoesNotContain("isDemo", names);
    }
}
