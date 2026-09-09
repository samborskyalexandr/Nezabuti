using Nezabuti.Api.Models;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Tests;

public class BillingCalendarTests
{
    [Fact]
    public void ResolvePaymentState_Unconfigured_WhenDatesMissing()
    {
        Assert.Equal(PaymentState.Unconfigured, BillingCalendar.ResolvePaymentState(null, null, DateTime.UtcNow));
        Assert.Equal(
            PaymentState.Unconfigured,
            BillingCalendar.ResolvePaymentState(DateTime.UtcNow, null, DateTime.UtcNow));
    }

    [Fact]
    public void ResolvePaymentState_Paid_OnPaidUntilDay()
    {
        var paidUntil = new DateTime(2027, 9, 8);
        var graceUntil = new DateTime(2027, 10, 8);

        Assert.Equal(
            PaymentState.Paid,
            BillingCalendar.ResolvePaymentState(paidUntil, graceUntil, new DateTime(2027, 9, 8)));
        Assert.Equal(
            PaymentState.Paid,
            BillingCalendar.ResolvePaymentState(paidUntil, graceUntil, new DateTime(2027, 1, 1)));
    }

    [Fact]
    public void ResolvePaymentState_Grace_BetweenPaidUntilAndGraceUntilInclusive()
    {
        var paidUntil = new DateTime(2027, 9, 8);
        var graceUntil = new DateTime(2027, 10, 8);

        Assert.Equal(
            PaymentState.Grace,
            BillingCalendar.ResolvePaymentState(paidUntil, graceUntil, new DateTime(2027, 9, 9)));
        Assert.Equal(
            PaymentState.Grace,
            BillingCalendar.ResolvePaymentState(paidUntil, graceUntil, new DateTime(2027, 10, 8)));
    }

    [Fact]
    public void ResolvePaymentState_Expired_AfterGraceUntil()
    {
        var paidUntil = new DateTime(2027, 9, 8);
        var graceUntil = new DateTime(2027, 10, 8);

        Assert.Equal(
            PaymentState.Expired,
            BillingCalendar.ResolvePaymentState(paidUntil, graceUntil, new DateTime(2027, 10, 9)));
    }

    [Fact]
    public void ComputeInitialPeriod_FromConfirmationDate()
    {
        var confirmed = new DateTime(2026, 9, 8, 15, 30, 0, DateTimeKind.Utc);
        var (from, to, grace) = BillingCalendar.ComputeInitialPeriod(confirmed);

        Assert.Equal(new DateTime(2026, 9, 8), from);
        Assert.Equal(new DateTime(2027, 9, 8), to);
        Assert.Equal(new DateTime(2027, 10, 8), grace);
    }

    [Fact]
    public void ComputeRenewalPeriod_WhenPaymentBeforePaidUntil_StartsFromPaidUntil()
    {
        var previousPaidUntil = new DateTime(2027, 9, 8);
        var paymentDate = new DateTime(2027, 9, 1);
        var (from, to, grace) = BillingCalendar.ComputeRenewalPeriod(previousPaidUntil, paymentDate);

        Assert.Equal(new DateTime(2027, 9, 8), from);
        Assert.Equal(new DateTime(2028, 9, 8), to);
        Assert.Equal(new DateTime(2028, 10, 8), grace);
    }

    [Fact]
    public void ComputeRenewalPeriod_WhenPaymentEqualsPaidUntil_StartsFromPaymentDate()
    {
        var previousPaidUntil = new DateTime(2027, 9, 8);
        var paymentDate = new DateTime(2027, 9, 8);
        var (from, to, grace) = BillingCalendar.ComputeRenewalPeriod(previousPaidUntil, paymentDate);

        Assert.Equal(new DateTime(2027, 9, 8), from);
        Assert.Equal(new DateTime(2028, 9, 8), to);
        Assert.Equal(new DateTime(2028, 10, 8), grace);
    }

    [Fact]
    public void ComputeRenewalPeriod_WhenPaymentInGrace_StartsFromPaymentDate()
    {
        var previousPaidUntil = new DateTime(2027, 9, 8);
        var paymentDate = new DateTime(2027, 10, 2);
        var (from, to, grace) = BillingCalendar.ComputeRenewalPeriod(previousPaidUntil, paymentDate);

        Assert.Equal(new DateTime(2027, 10, 2), from);
        Assert.Equal(new DateTime(2028, 10, 2), to);
        Assert.Equal(new DateTime(2028, 11, 2), grace);
    }

    [Fact]
    public void ComputeRenewalPeriod_WhenPaymentAfterGrace_StartsFromPaymentDate()
    {
        var previousPaidUntil = new DateTime(2027, 9, 8);
        var paymentDate = new DateTime(2027, 12, 20);
        var (from, to, grace) = BillingCalendar.ComputeRenewalPeriod(previousPaidUntil, paymentDate);

        Assert.Equal(new DateTime(2027, 12, 20), from);
        Assert.Equal(new DateTime(2028, 12, 20), to);
        Assert.Equal(new DateTime(2029, 1, 20), grace);
    }

    [Fact]
    public void ComputeRenewalPeriod_GraceUntil_IsPaidUntilPlusOneCalendarMonth()
    {
        var (_, to, grace) = BillingCalendar.ComputeRenewalPeriod(
            new DateTime(2027, 1, 31),
            new DateTime(2027, 3, 1));

        Assert.Equal(new DateTime(2028, 3, 1), to);
        Assert.Equal(BillingCalendar.AddGraceMonth(to), grace);
        Assert.Equal(new DateTime(2028, 4, 1), grace);
    }

    [Fact]
    public void StatusAfterSuccessfulPayment_SuspendedBecomesPublished_DraftStaysDraft()
    {
        Assert.Equal(MemorialStatus.Published, BillingCalendar.StatusAfterSuccessfulPayment(MemorialStatus.Suspended));
        Assert.Equal(MemorialStatus.Draft, BillingCalendar.StatusAfterSuccessfulPayment(MemorialStatus.Draft));
        Assert.Equal(MemorialStatus.Published, BillingCalendar.StatusAfterSuccessfulPayment(MemorialStatus.Published));
        Assert.Equal(MemorialStatus.Archived, BillingCalendar.StatusAfterSuccessfulPayment(MemorialStatus.Archived));
    }

    [Fact]
    public void ShouldSuspendPublished_OnlyWhenPastGrace_AndPublished_NotDemo()
    {
        var grace = new DateTime(2027, 10, 8);
        var afterGrace = new DateTime(2027, 10, 9);
        var onGrace = new DateTime(2027, 10, 8);

        Assert.True(BillingCalendar.ShouldSuspendPublished(MemorialStatus.Published, false, grace, afterGrace));
        Assert.False(BillingCalendar.ShouldSuspendPublished(MemorialStatus.Published, false, grace, onGrace));
        Assert.False(BillingCalendar.ShouldSuspendPublished(MemorialStatus.Draft, false, grace, afterGrace));
        Assert.False(BillingCalendar.ShouldSuspendPublished(MemorialStatus.Published, true, grace, afterGrace));
        Assert.False(BillingCalendar.ShouldSuspendPublished(MemorialStatus.Published, false, null, afterGrace));
        Assert.False(BillingCalendar.ShouldSuspendPublished(MemorialStatus.Archived, false, grace, afterGrace));
        Assert.False(BillingCalendar.ShouldSuspendPublished(MemorialStatus.Suspended, false, grace, afterGrace));
    }

    [Fact]
    public void IsEndingWithinDays_InclusiveWindow()
    {
        var today = new DateTime(2026, 9, 8);
        Assert.True(BillingCalendar.IsEndingWithinDays(today.AddDays(7), today, 7));
        Assert.True(BillingCalendar.IsEndingWithinDays(today, today, 7));
        Assert.False(BillingCalendar.IsEndingWithinDays(today.AddDays(8), today, 7));
        Assert.False(BillingCalendar.IsEndingWithinDays(today.AddDays(-1), today, 7));
        Assert.False(BillingCalendar.IsEndingWithinDays(null, today, 30));
    }

    [Fact]
    public void PaymentStateLabelUk_UsesUkrainianLabels()
    {
        Assert.Equal("Оплачено", BillingCalendar.PaymentStateLabelUk(PaymentState.Paid));
        Assert.Equal("Пільговий період", BillingCalendar.PaymentStateLabelUk(PaymentState.Grace));
        Assert.Equal("Прострочено", BillingCalendar.PaymentStateLabelUk(PaymentState.Expired));
        Assert.Equal("Оплату не налаштовано", BillingCalendar.PaymentStateLabelUk(PaymentState.Unconfigured));
        Assert.Equal("Призупинено", BillingCalendar.MemorialStatusLabelUk(MemorialStatus.Suspended));
    }

    [Fact]
    public void ResolvePaymentState_Paid_WhenTodayBeforePaidUntil()
    {
        var paidUntil = new DateTime(2027, 9, 8);
        var graceUntil = new DateTime(2027, 10, 8);
        Assert.Equal(
            PaymentState.Paid,
            BillingCalendar.ResolvePaymentState(paidUntil, graceUntil, new DateTime(2027, 9, 1)));
    }

    [Fact]
    public void ComputeRenewalPeriod_LeapYearAndMonthEnd()
    {
        // Leap day payment after paid-until
        var (from, to, grace) = BillingCalendar.ComputeRenewalPeriod(
            previousPaidUntil: new DateTime(2024, 1, 15),
            paymentConfirmedAt: new DateTime(2024, 2, 29));
        Assert.Equal(new DateTime(2024, 2, 29), from);
        Assert.Equal(new DateTime(2025, 2, 28), to);
        Assert.Equal(new DateTime(2025, 3, 28), grace);

        // Month-end start: Jan 31 + 1 year + 1 month
        var (_, to2, grace2) = BillingCalendar.ComputeRenewalPeriod(
            new DateTime(2027, 1, 31),
            new DateTime(2026, 12, 1));
        Assert.Equal(new DateTime(2028, 1, 31), to2);
        Assert.Equal(new DateTime(2028, 2, 29), grace2); // 2028 leap year
    }
}
