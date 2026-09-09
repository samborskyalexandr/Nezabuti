namespace Nezabuti.Api.Services;

/// <summary>
/// Pure helper: next daily billing run at local DailyRunHour in the billing timezone (DST-safe).
/// </summary>
public static class BillingSchedule
{
    /// <summary>
    /// If local time &lt; DailyRunHour → today at that hour; if local time &gt;= DailyRunHour → tomorrow.
    /// Returns UTC instant for Task.Delay targeting.
    /// </summary>
    public static DateTime GetNextRunUtc(DateTime utcNow, TimeZoneInfo timeZone, int dailyRunHour)
    {
        if (dailyRunHour is < 0 or > 23)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyRunHour), "DailyRunHour must be 0–23.");
        }

        var localNow = BillingTimeZone.ConvertTimeFromUtc(utcNow, timeZone);
        var todayRunLocal = new DateTime(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            dailyRunHour,
            0,
            0,
            DateTimeKind.Unspecified);

        var nextLocal = localNow < todayRunLocal
            ? todayRunLocal
            : todayRunLocal.AddDays(1);

        return TimeZoneInfo.ConvertTimeToUtc(nextLocal, timeZone);
    }

    public static TimeSpan GetDelayUntilNextRun(DateTime utcNow, TimeZoneInfo timeZone, int dailyRunHour)
    {
        var next = GetNextRunUtc(utcNow, timeZone, dailyRunHour);
        var delay = next - BillingTimeZone.ToUtc(utcNow);
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }
}
