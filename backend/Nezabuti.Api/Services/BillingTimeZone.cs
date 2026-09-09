namespace Nezabuti.Api.Services;

/// <summary>
/// Resolves billing timezone for Linux Docker (IANA) with a Windows fallback.
/// Prefer Europe/Kyiv — never rely on a fixed UTC offset.
/// </summary>
public static class BillingTimeZone
{
    public const string DefaultTimeZoneId = "Europe/Kyiv";
    private const string WindowsFallbackId = "FLE Standard Time";

    public static TimeZoneInfo Resolve(string? timeZoneId = null)
    {
        var id = string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId.Trim();
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            if (!string.Equals(id, DefaultTimeZoneId, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
                }
                catch (Exception)
                {
                    // fall through to Windows id / UTC
                }
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(WindowsFallbackId);
            }
            catch (Exception)
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    public static DateTime ToUtc(DateTime utcCandidate)
    {
        return utcCandidate.Kind switch
        {
            DateTimeKind.Utc => utcCandidate,
            DateTimeKind.Local => utcCandidate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcCandidate, DateTimeKind.Utc)
        };
    }

    /// <summary>Calendar date in the billing timezone for an instant.</summary>
    public static DateTime GetLocalDate(DateTime utcNow, TimeZoneInfo timeZone)
    {
        var utc = ToUtc(utcNow);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone).Date;
    }

    public static DateTime ConvertTimeFromUtc(DateTime utcNow, TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(ToUtc(utcNow), timeZone);
    }
}
