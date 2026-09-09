using Nezabuti.Api.Models;
using Nezabuti.Api.Repositories;

namespace Nezabuti.Api.Services;

public interface IMemorialBillingJobService
{
    Task ProcessAsync(CancellationToken ct = default);
}

public sealed class MemorialBillingJobService : IMemorialBillingJobService
{
    private readonly IMemorialRepository _memorials;
    private readonly ITelegramAdminNotifyService _telegram;
    private readonly IBillingClock _clock;
    private readonly ILogger<MemorialBillingJobService> _logger;

    public MemorialBillingJobService(
        IMemorialRepository memorials,
        ITelegramAdminNotifyService telegram,
        IBillingClock clock,
        ILogger<MemorialBillingJobService> logger)
    {
        _memorials = memorials;
        _telegram = telegram;
        _clock = clock;
        _logger = logger;
    }

    public async Task ProcessAsync(CancellationToken ct = default)
    {
        // Date-only comparisons in Europe/Kyiv (or configured TZ), not UTC midnight.
        var today = _clock.TodayLocal;
        List<Memorial> candidates;
        try
        {
            candidates = await _memorials.ListBillingCandidatesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load billing candidates");
            return;
        }

        foreach (var memorial in candidates)
        {
            try
            {
                await ProcessOneAsync(memorial, today, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Billing job failed for memorial {MemorialId}", memorial.Id);
            }
        }
    }

    private async Task ProcessOneAsync(Memorial memorial, DateTime today, CancellationToken ct)
    {
        var changed = false;

        if (BillingCalendar.ShouldSuspendPublished(memorial.Status, memorial.IsDemo, memorial.GraceUntil, today))
        {
            memorial.Status = MemorialStatus.Suspended;
            changed = true;
        }

        if (memorial.Status == MemorialStatus.Suspended && memorial.SuspendedReminderSentAt is null)
        {
            var (ok, _) = await _telegram.SendBillingReminderAsync(memorial, "suspended", ct);
            if (ok)
            {
                memorial.SuspendedReminderSentAt = _clock.UtcNow;
                changed = true;
            }
        }

        // Reminders only for non-demo with configured dates (already filtered).
        if (!memorial.IsDemo && memorial.PaidUntil is not null && memorial.GraceUntil is not null
            && memorial.Status != MemorialStatus.Suspended)
        {
            changed |= await TryReminderAsync(
                memorial,
                "reminder30",
                shouldSend: memorial.Reminder30SentAt is null
                            && BillingCalendar.DaysUntil(memorial.PaidUntil.Value, today) is >= 8 and <= 30,
                mark: m => m.Reminder30SentAt = _clock.UtcNow,
                ct);

            changed |= await TryReminderAsync(
                memorial,
                "reminder7",
                shouldSend: memorial.Reminder7SentAt is null
                            && BillingCalendar.DaysUntil(memorial.PaidUntil.Value, today) is >= 1 and <= 7,
                mark: m => m.Reminder7SentAt = _clock.UtcNow,
                ct);

            changed |= await TryReminderAsync(
                memorial,
                "expired",
                shouldSend: memorial.ExpiredReminderSentAt is null
                            && today.Date == memorial.PaidUntil.Value.Date,
                mark: m => m.ExpiredReminderSentAt = _clock.UtcNow,
                ct);

            changed |= await TryReminderAsync(
                memorial,
                "grace7",
                shouldSend: memorial.Grace7ReminderSentAt is null
                            && today.Date > memorial.PaidUntil.Value.Date
                            && today.Date <= memorial.GraceUntil.Value.Date
                            && BillingCalendar.DaysUntil(memorial.GraceUntil.Value, today) is >= 0 and <= 7,
                mark: m => m.Grace7ReminderSentAt = _clock.UtcNow,
                ct);
        }

        if (changed)
        {
            await _memorials.ReplaceAsync(memorial, ct);
        }
    }

    private async Task<bool> TryReminderAsync(
        Memorial memorial,
        string kind,
        bool shouldSend,
        Action<Memorial> mark,
        CancellationToken ct)
    {
        if (!shouldSend)
        {
            return false;
        }

        var (ok, _) = await _telegram.SendBillingReminderAsync(memorial, kind, ct);
        if (!ok)
        {
            return false;
        }

        mark(memorial);
        return true;
    }
}
