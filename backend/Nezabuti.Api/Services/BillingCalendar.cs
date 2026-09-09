using Nezabuti.Api.Models;

namespace Nezabuti.Api.Services;

/// <summary>
/// Pure date/billing helpers (unit-testable, no I/O).
/// Date comparisons treat PaidUntil/GraceUntil as calendar dates; pass "today" in Europe/Kyiv.
/// </summary>
public static class BillingCalendar
{
    public static DateTime AddOneYear(DateTime fromDate) => fromDate.Date.AddYears(1);

    public static DateTime AddGraceMonth(DateTime paidUntilDate) => paidUntilDate.Date.AddMonths(1);

    public static PaymentState ResolvePaymentState(DateTime? paidUntil, DateTime? graceUntil, DateTime todayLocal)
    {
        if (paidUntil is null || graceUntil is null)
        {
            return PaymentState.Unconfigured;
        }

        var today = todayLocal.Date;
        var paid = paidUntil.Value.Date;
        var grace = graceUntil.Value.Date;

        if (today <= paid)
        {
            return PaymentState.Paid;
        }

        if (today <= grace)
        {
            return PaymentState.Grace;
        }

        return PaymentState.Expired;
    }

    /// <summary>
    /// Renewal: periodStart = max(previous PaidUntil, payment confirmation date).
    /// GraceUntil always = new PaidUntil + 1 calendar month (no separate grace branch).
    /// </summary>
    public static (DateTime PeriodFrom, DateTime PeriodTo, DateTime GraceUntil) ComputeRenewalPeriod(
        DateTime previousPaidUntil,
        DateTime paymentConfirmedAt)
    {
        var previous = previousPaidUntil.Date;
        var paymentDate = paymentConfirmedAt.Date;
        var periodFrom = previous >= paymentDate ? previous : paymentDate;
        var periodTo = AddOneYear(periodFrom);
        var grace = AddGraceMonth(periodTo);
        return (periodFrom, periodTo, grace);
    }

    /// <summary>
    /// First payment: year starts at confirmation date.
    /// </summary>
    public static (DateTime PeriodFrom, DateTime PeriodTo, DateTime GraceUntil) ComputeInitialPeriod(DateTime confirmedAt)
    {
        var periodFrom = confirmedAt.Date;
        var periodTo = AddOneYear(periodFrom);
        var grace = AddGraceMonth(periodTo);
        return (periodFrom, periodTo, grace);
    }

    public static string PaymentStateLabelUk(PaymentState state) => state switch
    {
        PaymentState.Paid => "Оплачено",
        PaymentState.Grace => "Пільговий період",
        PaymentState.Expired => "Прострочено",
        _ => "Оплату не налаштовано"
    };

    public static string MemorialStatusLabelUk(MemorialStatus status) => status switch
    {
        MemorialStatus.Draft => "Чернетка",
        MemorialStatus.Published => "Опубліковано",
        MemorialStatus.Suspended => "Призупинено",
        MemorialStatus.Archived => "В архіві",
        _ => status.ToString()
    };

    /// <summary>
    /// Published pages past GraceUntil become Suspended. Legacy without dates — never auto-suspend.
    /// Demo/Archived/Draft are never suspended by this rule.
    /// </summary>
    public static bool ShouldSuspendPublished(MemorialStatus status, bool isDemo, DateTime? graceUntil, DateTime todayLocal)
    {
        if (isDemo || status != MemorialStatus.Published || graceUntil is null)
        {
            return false;
        }

        return todayLocal.Date > graceUntil.Value.Date;
    }

    public static int DaysUntil(DateTime targetDate, DateTime todayLocal) =>
        (targetDate.Date - todayLocal.Date).Days;

    /// <summary>PaidUntil within [today, today+days] inclusive.</summary>
    public static bool IsEndingWithinDays(DateTime? paidUntil, DateTime todayLocal, int days)
    {
        if (paidUntil is null || days < 0)
        {
            return false;
        }

        var today = todayLocal.Date;
        var paid = paidUntil.Value.Date;
        if (paid < today)
        {
            return false;
        }

        return paid <= today.AddDays(days);
    }

    /// <summary>
    /// Pure helper for publication status after a successful payment apply.
    /// Suspended → Published; Draft/Archive/Published unchanged.
    /// </summary>
    public static MemorialStatus StatusAfterSuccessfulPayment(MemorialStatus current) =>
        current == MemorialStatus.Suspended ? MemorialStatus.Published : current;
}
