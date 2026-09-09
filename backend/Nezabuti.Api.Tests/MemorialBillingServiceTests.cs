using Nezabuti.Api.Models;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Tests;

public class MemorialBillingServiceTests
{
    [Fact]
    public async Task ConfirmInitialPayment_CreatesPayment_AndSetsPeriod()
    {
        var memorials = new FakeMemorialRepository();
        var payments = new FakeMemorialPaymentRepository();
        var clock = new FakeBillingClock
        {
            TodayLocal = new DateTime(2026, 9, 9),
            UtcNow = new DateTime(2026, 9, 9, 7, 30, 0, DateTimeKind.Utc)
        };
        var memorial = BillingTestFixtures.CreateMemorial(MemorialStatus.Draft, paidUntil: null, graceUntil: null);
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateBillingService(memorials, payments, clock);

        await sut.ConfirmInitialPaymentAsync(memorial.Id, amount: 700m, note: "cash");

        var payment = Assert.Single(payments.Payments);
        Assert.Equal(MemorialPaymentType.Initial, payment.Type);
        Assert.Equal(700m, payment.Amount);
        Assert.Equal(clock.UtcNow, payment.PaidAt);
        Assert.Equal(new DateTime(2026, 9, 9), payment.PeriodFrom);
        Assert.Equal(new DateTime(2027, 9, 9), payment.PeriodTo);

        var updated = memorials.Items[memorial.Id];
        Assert.Equal(new DateTime(2027, 9, 9), updated.PaidUntil);
        Assert.Equal(new DateTime(2027, 10, 9), updated.GraceUntil);
        Assert.Equal(clock.UtcNow, updated.LastPaymentAt);
        Assert.Equal(MemorialStatus.Draft, updated.Status);
        Assert.Equal(PaymentStatus.Paid, updated.PaymentStatus);
    }

    [Fact]
    public async Task ConfirmRenewal_UsesMaxOfPaidUntilAndPaymentDate()
    {
        var memorials = new FakeMemorialRepository();
        var payments = new FakeMemorialPaymentRepository();
        var clock = new FakeBillingClock
        {
            TodayLocal = new DateTime(2027, 10, 2),
            UtcNow = new DateTime(2027, 10, 2, 8, 0, 0, DateTimeKind.Utc)
        };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Published,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorial.Reminder30SentAt = clock.UtcNow.AddDays(-40);
        memorial.Reminder7SentAt = clock.UtcNow.AddDays(-10);
        memorial.ExpiredReminderSentAt = clock.UtcNow.AddDays(-5);
        memorial.Grace7ReminderSentAt = clock.UtcNow.AddDays(-1);
        memorial.SuspendedReminderSentAt = clock.UtcNow;
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateBillingService(memorials, payments, clock);

        await sut.ConfirmRenewalAsync(memorial.Id, amount: 300m);

        var payment = Assert.Single(payments.Payments);
        Assert.Equal(MemorialPaymentType.Renewal, payment.Type);
        Assert.Equal(new DateTime(2027, 10, 2), payment.PeriodFrom);
        Assert.Equal(new DateTime(2028, 10, 2), payment.PeriodTo);
        Assert.Equal(300m, payment.Amount);

        var updated = memorials.Items[memorial.Id];
        Assert.Equal(new DateTime(2028, 10, 2), updated.PaidUntil);
        Assert.Equal(new DateTime(2028, 11, 2), updated.GraceUntil);
        Assert.Equal(clock.UtcNow, updated.LastPaymentAt);
        Assert.Null(updated.Reminder30SentAt);
        Assert.Null(updated.Reminder7SentAt);
        Assert.Null(updated.ExpiredReminderSentAt);
        Assert.Null(updated.Grace7ReminderSentAt);
        Assert.Null(updated.SuspendedReminderSentAt);
    }

    [Fact]
    public async Task ConfirmRenewal_WhenPaymentBeforePaidUntil_ExtendsFromPaidUntil()
    {
        var memorials = new FakeMemorialRepository();
        var payments = new FakeMemorialPaymentRepository();
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 9, 1), UtcNow = new DateTime(2027, 9, 1, 10, 0, 0, DateTimeKind.Utc) };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Published,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateBillingService(memorials, payments, clock);

        await sut.ConfirmRenewalAsync(memorial.Id);

        var payment = Assert.Single(payments.Payments);
        Assert.Equal(new DateTime(2027, 9, 8), payment.PeriodFrom);
        Assert.Equal(new DateTime(2028, 9, 8), payment.PeriodTo);
        Assert.Equal(new DateTime(2028, 10, 8), memorials.Items[memorial.Id].GraceUntil);
    }

    [Fact]
    public async Task ConfirmRenewal_Suspended_BecomesPublished()
    {
        var memorials = new FakeMemorialRepository();
        var payments = new FakeMemorialPaymentRepository();
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 12, 20), UtcNow = new DateTime(2027, 12, 20, 9, 0, 0, DateTimeKind.Utc) };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Suspended,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateBillingService(memorials, payments, clock);

        await sut.ConfirmRenewalAsync(memorial.Id);

        Assert.Equal(MemorialStatus.Published, memorials.Items[memorial.Id].Status);
        Assert.NotNull(memorials.Items[memorial.Id].PublishedAt);
    }

    [Fact]
    public async Task ConfirmRenewal_Draft_StaysDraft()
    {
        var memorials = new FakeMemorialRepository();
        var payments = new FakeMemorialPaymentRepository();
        var clock = new FakeBillingClock { TodayLocal = new DateTime(2027, 9, 1), UtcNow = new DateTime(2027, 9, 1, 9, 0, 0, DateTimeKind.Utc) };
        var memorial = BillingTestFixtures.CreateMemorial(
            MemorialStatus.Draft,
            paidUntil: new DateTime(2027, 9, 8),
            graceUntil: new DateTime(2027, 10, 8));
        memorials.Seed(memorial);
        var sut = BillingTestFixtures.CreateBillingService(memorials, payments, clock);

        await sut.ConfirmRenewalAsync(memorial.Id);

        Assert.Equal(MemorialStatus.Draft, memorials.Items[memorial.Id].Status);
    }
}
