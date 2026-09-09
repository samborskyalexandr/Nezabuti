using Nezabuti.Api.Services;

namespace Nezabuti.Api.Tests;

public class BillingScheduleTests
{
    private static readonly TimeZoneInfo Kyiv = BillingTimeZone.Resolve("Europe/Kyiv");

    [Theory]
    [InlineData(2026, 9, 9, 8, 0, 2026, 9, 9)]   // 08:00 → today
    [InlineData(2026, 9, 9, 9, 59, 2026, 9, 9)]  // 09:59 → today
    [InlineData(2026, 9, 9, 10, 0, 2026, 9, 10)] // 10:00 → tomorrow
    [InlineData(2026, 9, 9, 10, 1, 2026, 9, 10)] // 10:01 → tomorrow
    [InlineData(2026, 9, 9, 15, 0, 2026, 9, 10)] // 15:00 → tomorrow
    [InlineData(2026, 9, 9, 23, 0, 2026, 9, 10)] // 23:00 → tomorrow
    public void GetNextRunUtc_UsesLocalHourInKyiv(
        int y, int m, int d, int hour, int minute,
        int expectedY, int expectedM, int expectedD)
    {
        var local = new DateTime(y, m, d, hour, minute, 0, DateTimeKind.Unspecified);
        var utcNow = TimeZoneInfo.ConvertTimeToUtc(local, Kyiv);

        var nextUtc = BillingSchedule.GetNextRunUtc(utcNow, Kyiv, dailyRunHour: 10);
        var nextLocal = TimeZoneInfo.ConvertTimeFromUtc(nextUtc, Kyiv);

        Assert.Equal(new DateTime(expectedY, expectedM, expectedD, 10, 0, 0), nextLocal);
    }

    [Fact]
    public void GetNextRunUtc_AcrossDstSpringForward_StillLocal10()
    {
        // Ukraine DST spring 2026: last Sunday of March — clocks jump 03:00 → 04:00.
        // On 2026-03-29 at 15:00 local, next run should be 2026-03-30 10:00 local.
        var local = new DateTime(2026, 3, 29, 15, 0, 0, DateTimeKind.Unspecified);
        var utcNow = TimeZoneInfo.ConvertTimeToUtc(local, Kyiv);
        var nextUtc = BillingSchedule.GetNextRunUtc(utcNow, Kyiv, 10);
        var nextLocal = TimeZoneInfo.ConvertTimeFromUtc(nextUtc, Kyiv);

        Assert.Equal(new DateTime(2026, 3, 30, 10, 0, 0), nextLocal);
    }

    [Fact]
    public void GetNextRunUtc_AcrossDstFallBack_StillLocal10()
    {
        // Ukraine DST fall 2026: last Sunday of October — 04:00 → 03:00.
        // On 2026-10-25 at 08:00 local, next run should be same day 10:00 local.
        var local = new DateTime(2026, 10, 25, 8, 0, 0, DateTimeKind.Unspecified);
        var utcNow = TimeZoneInfo.ConvertTimeToUtc(local, Kyiv);
        var nextUtc = BillingSchedule.GetNextRunUtc(utcNow, Kyiv, 10);
        var nextLocal = TimeZoneInfo.ConvertTimeFromUtc(nextUtc, Kyiv);

        Assert.Equal(new DateTime(2026, 10, 25, 10, 0, 0), nextLocal);
    }

    [Fact]
    public void GetDelayUntilNextRun_IsNonNegative()
    {
        var utcNow = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);
        var delay = BillingSchedule.GetDelayUntilNextRun(utcNow, Kyiv, 10);
        Assert.True(delay >= TimeSpan.Zero);
    }

    [Fact]
    public void BillingTimeZone_ResolvesEuropeKyiv_AndUsesLocalCalendarDate()
    {
        var tz = BillingTimeZone.Resolve("Europe/Kyiv");
        Assert.NotNull(tz);

        // 22:30 UTC on Sept 8 → already Sept 9 in Kyiv (UTC+3 in September).
        var localDate = BillingTimeZone.GetLocalDate(new DateTime(2026, 9, 8, 22, 30, 0, DateTimeKind.Utc), tz);
        Assert.Equal(new DateTime(2026, 9, 9), localDate);
    }
}
