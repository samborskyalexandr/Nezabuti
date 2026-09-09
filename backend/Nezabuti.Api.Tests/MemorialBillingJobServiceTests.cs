using Nezabuti.Api.Models;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Tests;

public class MemorialBillingJobServiceTests
{
    [Fact]
    public async Task Process_PublishedPastGrace_Suspends()
    {
        var memorials = new FakeMemorialRepository();
        var telegram = new FakeTelegramAdminNotifyService();
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 10, 9) };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Published,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateJobService(memorials, telegram, clock);

        await sut.ProcessAsync();

        Assert.Equal(MemorialStatus.Suspended, memorials.Items[memorial.Id].Status);
        Assert.Contains(telegram.BillingCalls, c => c.Kind == "suspended");
    }

    [Fact]
    public async Task Process_PublishedPaid_StaysPublished()
    {
        var memorials = new FakeMemorialRepository();
        var telegram = new FakeTelegramAdminNotifyService();
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 9, 1) };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Published,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateJobService(memorials, telegram, clock);

        await sut.ProcessAsync();

        Assert.Equal(MemorialStatus.Published, memorials.Items[memorial.Id].Status);
    }

    [Fact]
    public async Task Process_PublishedInGrace_StaysPublished()
    {
        var memorials = new FakeMemorialRepository();
        var telegram = new FakeTelegramAdminNotifyService();
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 9, 15) };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Published,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateJobService(memorials, telegram, clock);

        await sut.ProcessAsync();

        Assert.Equal(MemorialStatus.Published, memorials.Items[memorial.Id].Status);
        Assert.DoesNotContain(telegram.BillingCalls, c => c.Kind == "suspended");
    }

    [Fact]
    public async Task Process_DraftExpired_StaysDraft_NoSuspend()
    {
        var memorials = new FakeMemorialRepository();
        var telegram = new FakeTelegramAdminNotifyService();
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 10, 9) };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Draft,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateJobService(memorials, telegram, clock);

        await sut.ProcessAsync();

        Assert.Equal(MemorialStatus.Draft, memorials.Items[memorial.Id].Status);
        Assert.DoesNotContain(telegram.BillingCalls, c => c.Kind == "suspended");
    }

    [Fact]
    public async Task Process_IsDemo_NeverAutoSuspends_EvenPastGrace()
    {
        var memorials = new FakeMemorialRepository();
        var telegram = new FakeTelegramAdminNotifyService();
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 10, 9) };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Published,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8),
            isDemo: true);
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateJobService(memorials, telegram, clock);

        await sut.ProcessAsync();

        // Demo filtered out of candidates — status unchanged, no telegram.
        Assert.Equal(MemorialStatus.Published, memorials.Items[memorial.Id].Status);
        Assert.Empty(telegram.BillingCalls);
    }

    [Fact]
    public async Task Process_LegacyWithoutPaidUntil_NotInCandidates()
    {
        var memorials = new FakeMemorialRepository();
        var telegram = new FakeTelegramAdminNotifyService();
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 10, 9) };
        var memorial = BillingTestFixtures.CreateMemorial(MemorialStatus.Published, paidUntil: null, graceUntil: null);
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateJobService(memorials, telegram, clock);

        await sut.ProcessAsync();

        Assert.Equal(MemorialStatus.Published, memorials.Items[memorial.Id].Status);
        Assert.Empty(telegram.BillingCalls);
    }

    [Theory]
    [InlineData("reminder30", 20)]
    [InlineData("reminder7", 5)]
    [InlineData("expired", 0)]
    public async Task Process_ReminderKinds_SendOnce(string expectedKind, int daysUntilPaid)
    {
        var paidUntil = new DateTime(2027, 9, 8);
        var today = paidUntil.AddDays(-daysUntilPaid);
        var memorials = new FakeMemorialRepository();
        var telegram = new FakeTelegramAdminNotifyService();
        var clock = new FakeBillingClock { TodayLocal = today, UtcNow = today.AddHours(7) };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Published,
            paidUntil: paidUntil,
            graceUntil: paidUntil.AddMonths(1));
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateJobService(memorials, telegram, clock);

        await sut.ProcessAsync();
        Assert.Contains(telegram.BillingCalls, c => c.Kind == expectedKind && c.Memorial.Id == memorial.Id);

        telegram.BillingCalls.Clear();
        await sut.ProcessAsync();
        Assert.DoesNotContain(telegram.BillingCalls, c => c.Kind == expectedKind);
    }

    [Fact]
    public async Task Process_Grace7Reminder_SendOnce()
    {
        var memorials = new FakeMemorialRepository();
        var telegram = new FakeTelegramAdminNotifyService();
        var paidUntil = new DateTime(2027, 9, 8);
        var graceUntil = new DateTime(2027, 10, 8);
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 10, 5), UtcNow = new DateTime(2027, 10, 5, 7, 0, 0, DateTimeKind.Utc) };
        var memorial = BillingTestFixtures.CreateMemorial(MemorialStatus.Published, paidUntil, graceUntil);
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateJobService(memorials, telegram, clock);

        await sut.ProcessAsync();
        Assert.Contains(telegram.BillingCalls, c => c.Kind == "grace7");
        Assert.NotNull(memorials.Items[memorial.Id].Grace7ReminderSentAt);

        telegram.BillingCalls.Clear();
        await sut.ProcessAsync();
        Assert.DoesNotContain(telegram.BillingCalls, c => c.Kind == "grace7");
    }

    [Fact]
    public async Task Process_SuspendedReminder_SendOnce()
    {
        var memorials = new FakeMemorialRepository();
        var telegram = new FakeTelegramAdminNotifyService();
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 10, 9), UtcNow = new DateTime(2027, 10, 9, 7, 0, 0, DateTimeKind.Utc) };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Suspended,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateJobService(memorials, telegram, clock);

        await sut.ProcessAsync();
        Assert.Single(telegram.BillingCalls, c => c.Kind == "suspended");
        Assert.NotNull(memorials.Items[memorial.Id].SuspendedReminderSentAt);

        telegram.BillingCalls.Clear();
        await sut.ProcessAsync();
        Assert.Empty(telegram.BillingCalls);
    }

    [Fact]
    public async Task Process_TelegramDisabled_DoesNotMarkFlags()
    {
        var memorials = new FakeMemorialRepository();
        var telegram = new FakeTelegramAdminNotifyService { Enabled = false };
        var paidUntil = new DateTime(2027, 9, 8);
        var clock = new FakeBillingClock { TodayLocal = paidUntil.AddDays(-5), UtcNow = paidUntil.AddDays(-5).AddHours(7) };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Published,
            paidUntil,
            paidUntil.AddMonths(1));
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateJobService(memorials, telegram, clock);

        await sut.ProcessAsync();

        Assert.Single(telegram.BillingCalls);
        Assert.Null(memorials.Items[memorial.Id].Reminder7SentAt);
    }

    [Fact]
    public async Task Process_TelegramException_ContinuesOtherMemorials()
    {
        var memorials = new FakeMemorialRepository();
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 10, 9), UtcNow = new DateTime(2027, 10, 9, 7, 0, 0, DateTimeKind.Utc) };

        var failing = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Published,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        failing.FullName = "Failing";

        var ok = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Published,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        ok.FullName = "Ok";

        memorials.Seed(failing);
        memorials.Seed(ok);

        var telegram = new SelectiveThrowTelegram(failing.Id);
        var sut = BillingTestFixtures.CreateJobService(memorials, telegram, clock);

        var ex = await Record.ExceptionAsync(() => sut.ProcessAsync());
        Assert.Null(ex);

        Assert.Equal(MemorialStatus.Suspended, memorials.Items[ok.Id].Status);
        Assert.Contains(telegram.BillingCalls, c => c.Memorial.Id == ok.Id && c.Kind == "suspended");
    }

    private sealed class SelectiveThrowTelegram : ITelegramAdminNotifyService
    {
        private readonly string _failId;
        public List<(Memorial Memorial, string Kind)> BillingCalls { get; } = [];

        public SelectiveThrowTelegram(string failId) => _failId = failId;

        public Task<(bool Ok, string Message)> SendMessageAsync(string text, CancellationToken ct = default) =>
            Task.FromResult((true, "ok"));

        public Task<(bool Ok, string Message)> SendTestMessageAsync(CancellationToken ct = default) =>
            SendMessageAsync("test", ct);

        public Task<(bool Ok, string Message)> SendBillingReminderAsync(
            Memorial memorial,
            string eventKind,
            CancellationToken ct = default)
        {
            if (memorial.Id == _failId)
            {
                throw new InvalidOperationException("fail one");
            }

            BillingCalls.Add((memorial, eventKind));
            return Task.FromResult((true, "ok"));
        }
    }
}
