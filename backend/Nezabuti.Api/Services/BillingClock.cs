using Microsoft.Extensions.Options;
using Nezabuti.Api.Configuration;

namespace Nezabuti.Api.Services;

public interface IBillingClock
{
    DateTime UtcNow { get; }

    /// <summary>Today's calendar date in the configured billing timezone (time = 00:00).</summary>
    DateTime TodayLocal { get; }

    TimeZoneInfo TimeZone { get; }

    int DailyRunHour { get; }
}

public sealed class BillingClock : IBillingClock
{
    private readonly TimeProvider _time;
    private readonly BillingSettings _settings;
    private readonly TimeZoneInfo _timeZone;

    public BillingClock(TimeProvider time, IOptions<BillingSettings> settings)
    {
        _time = time;
        _settings = settings.Value;
        _timeZone = BillingTimeZone.Resolve(_settings.TimeZoneId);
    }

    public DateTime UtcNow => _time.GetUtcNow().UtcDateTime;

    public DateTime TodayLocal => BillingTimeZone.GetLocalDate(UtcNow, _timeZone);

    public TimeZoneInfo TimeZone => _timeZone;

    public int DailyRunHour =>
        _settings.DailyRunHour is >= 0 and <= 23 ? _settings.DailyRunHour : 10;
}
