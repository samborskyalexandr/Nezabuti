using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Models.Blocks;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Tests;

public class BillingReminderMessageBuilderTests
{
    [Fact]
    public void Build_ContainsName_PaidUntil_Reason_AndAdminUrl_WithoutCustomerContacts()
    {
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Published,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorial.FullName = "Іван Петренко";
        memorial.CustomerId = BillingTestFixtures.NewId();

        var adminUrl = "http://localhost:8088/manage-nz7k4p/memorials/" + memorial.Id;
        var text = BillingReminderMessageBuilder.Build(
            memorial,
            "reminder30",
            todayLocal: new DateTime(2027, 8, 10),
            adminMemorialUrl: adminUrl);

        Assert.Contains("Іван Петренко", text);
        Assert.Contains("08.09.2027", text);
        Assert.Contains("30 днів", text);
        Assert.Contains(adminUrl, text);
        Assert.DoesNotContain("@", text);
        Assert.DoesNotContain("phone", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("+380", text);
    }

    [Fact]
    public void Build_Suspended_ContainsReason()
    {
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Suspended,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorial.FullName = "Тест";

        var text = BillingReminderMessageBuilder.Build(
            memorial,
            "suspended",
            new DateTime(2027, 10, 9),
            "http://localhost:8088/manage-nz7k4p/memorials/x");

        Assert.Contains("призупинено", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Тест", text);
    }

    [Theory]
    [InlineData(MemorialStatus.Suspended, "2027-09-08", "2027-10-08", "2027-10-09", "suspended")]
    [InlineData(MemorialStatus.Published, "2027-09-08", "2027-10-08", "2027-08-20", "reminder30")]
    [InlineData(MemorialStatus.Published, "2027-09-08", "2027-10-08", "2027-09-03", "reminder7")]
    [InlineData(MemorialStatus.Published, "2027-09-08", "2027-10-08", "2027-09-08", "expired")]
    [InlineData(MemorialStatus.Published, "2027-09-08", "2027-10-08", "2027-10-01", "grace7")]
    public void ResolveSampleReminderKind_MatchesBillingState(
        MemorialStatus status,
        string paidUntil,
        string graceUntil,
        string today,
        string expectedKind)
    {
        var memorial = BillingTestFixtures.CreateMemorial(
            status,
            DateTime.Parse(paidUntil),
            DateTime.Parse(graceUntil));

        var kind = TelegramAdminNotifyService.ResolveSampleReminderKind(memorial, DateTime.Parse(today));
        Assert.Equal(expectedKind, kind);
    }
}

public class PublicBillingPrivacyTests
{
    [Fact]
    public void PublicMemorialDto_HasNoCustomerOrPrivateBillingFields()
    {
        var names = typeof(PublicMemorialDto).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("IsTemporarilyUnavailable", names);
        Assert.DoesNotContain("Customer", names);
        Assert.DoesNotContain("CustomerId", names);
        Assert.DoesNotContain("PaidUntil", names);
        Assert.DoesNotContain("GraceUntil", names);
        Assert.DoesNotContain("PaymentState", names);
        Assert.DoesNotContain("FinalPrice", names);
        Assert.DoesNotContain("LastPaymentAt", names);
        Assert.DoesNotContain("TelegramBotToken", names);
        Assert.DoesNotContain("TelegramChatId", names);
    }

    [Fact]
    public void SiteSettingsDto_DoesNotExposeRawTelegramToken()
    {
        var names = typeof(SiteSettingsDto).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("TelegramBotTokenMasked", names);
        Assert.DoesNotContain("TelegramBotToken", names);
        Assert.DoesNotContain("TelegramBotTokenEnc", names);
    }

    [Fact]
    public async Task GetPublicAsync_Suspended_ReturnsTemporarilyUnavailable_NotNull()
    {
        var memorials = new FakeMemorialRepository();
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Suspended,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorial.PublicId = "SUSPENDED01";
        memorial.Blocks.Add(new MemorialBlock
        {
            Id = BillingTestFixtures.NewId(),
            Type = "Text",
            Order = 0
        });
        memorials.Seed(memorial);

        var sut = BillingTestFixtures.CreateMemorialService(memorials);
        var dto = await sut.GetPublicAsync("SUSPENDED01");

        Assert.NotNull(dto);
        Assert.True(dto!.IsTemporarilyUnavailable);
        Assert.Empty(dto.Blocks);
        Assert.Equal("SUSPENDED01", dto.PublicId);
        Assert.Null(dto.Callsign);
        Assert.Null(dto.ShortText);
    }

    [Fact]
    public async Task GetPublicAsync_Draft_ReturnsNull()
    {
        var memorials = new FakeMemorialRepository();
        var memorial = BillingTestFixtures.CreateMemorial(MemorialStatus.Draft);
        memorial.PublicId = "DRAFT00001";
        memorials.Seed(memorial);

        var sut = BillingTestFixtures.CreateMemorialService(memorials);
        var dto = await sut.GetPublicAsync("DRAFT00001");
        Assert.Null(dto);
    }
}
