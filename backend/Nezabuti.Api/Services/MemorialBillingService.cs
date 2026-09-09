using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Repositories;

namespace Nezabuti.Api.Services;

public interface IMemorialBillingService
{
    Task ConfirmInitialPaymentAsync(
        string memorialId,
        decimal? amount = null,
        string? note = null,
        string? createdBy = null,
        CancellationToken ct = default);

    Task ConfirmRenewalAsync(
        string memorialId,
        decimal? amount = null,
        string? note = null,
        string? createdBy = null,
        CancellationToken ct = default);

    Task<List<MemorialPaymentDto>> ListPaymentsAsync(string memorialId, CancellationToken ct = default);
}

public sealed class MemorialBillingService : IMemorialBillingService
{
    private readonly IMemorialRepository _memorials;
    private readonly IMemorialPaymentRepository _payments;
    private readonly IBillingClock _clock;

    public MemorialBillingService(
        IMemorialRepository memorials,
        IMemorialPaymentRepository payments,
        IBillingClock clock)
    {
        _memorials = memorials;
        _payments = payments;
        _clock = clock;
    }

    public async Task ConfirmInitialPaymentAsync(
        string memorialId,
        decimal? amount = null,
        string? note = null,
        string? createdBy = null,
        CancellationToken ct = default)
    {
        var memorial = await _memorials.GetByIdAsync(memorialId, ct)
            ?? throw new InvalidOperationException("Меморіал не знайдено.");

        var now = _clock.UtcNow;
        var confirmationDate = _clock.TodayLocal;
        var (periodFrom, periodTo, graceUntil) = BillingCalendar.ComputeInitialPeriod(confirmationDate);
        var paidAmount = amount ?? MemorialPricing.ResolveDefaultInitialAmount(memorial);

        await ApplyPaymentAsync(
            memorial,
            MemorialPaymentType.Initial,
            paidAmount,
            now,
            periodFrom,
            periodTo,
            graceUntil,
            note,
            createdBy,
            ct);
    }

    public async Task ConfirmRenewalAsync(
        string memorialId,
        decimal? amount = null,
        string? note = null,
        string? createdBy = null,
        CancellationToken ct = default)
    {
        var memorial = await _memorials.GetByIdAsync(memorialId, ct)
            ?? throw new InvalidOperationException("Меморіал не знайдено.");

        if (memorial.PaidUntil is null)
        {
            throw new InvalidOperationException(
                "Немає попередньої дати «Оплачено до». Спочатку підтвердіть первинну оплату.");
        }

        var now = _clock.UtcNow;
        var confirmationDate = _clock.TodayLocal;
        var (periodFrom, periodTo, graceUntil) = BillingCalendar.ComputeRenewalPeriod(
            memorial.PaidUntil.Value,
            confirmationDate);
        var paidAmount = amount ?? MemorialPricing.ResolveDefaultRenewalAmount(memorial);

        await ApplyPaymentAsync(
            memorial,
            MemorialPaymentType.Renewal,
            paidAmount,
            now,
            periodFrom,
            periodTo,
            graceUntil,
            note,
            createdBy,
            ct);
    }

    public async Task<List<MemorialPaymentDto>> ListPaymentsAsync(string memorialId, CancellationToken ct = default)
    {
        var payments = await _payments.ListByMemorialIdAsync(memorialId, ct);
        return payments.Select(MapPayment).ToList();
    }

    private async Task ApplyPaymentAsync(
        Memorial memorial,
        MemorialPaymentType type,
        decimal amount,
        DateTime paidAt,
        DateTime periodFrom,
        DateTime periodTo,
        DateTime graceUntil,
        string? note,
        string? createdBy,
        CancellationToken ct)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException("Сума оплати не може бути від'ємною.");
        }

        var payment = new MemorialPayment
        {
            MemorialId = memorial.Id,
            CustomerId = memorial.CustomerId,
            Amount = amount,
            PaidAt = paidAt,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            Type = type,
            Method = MemorialPaymentMethod.Manual,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? null : createdBy.Trim()
        };

        await _payments.CreateAsync(payment, ct);

        memorial.PaidUntil = periodTo;
        memorial.GraceUntil = graceUntil;
        memorial.LastPaymentAt = paidAt;
        memorial.PaidAt = paidAt;
        memorial.PaymentStatus = PaymentStatus.Paid;
        memorial.Reminder30SentAt = null;
        memorial.Reminder7SentAt = null;
        memorial.ExpiredReminderSentAt = null;
        memorial.Grace7ReminderSentAt = null;
        memorial.SuspendedReminderSentAt = null;

        var previousStatus = memorial.Status;
        memorial.Status = BillingCalendar.StatusAfterSuccessfulPayment(previousStatus);
        if (previousStatus == MemorialStatus.Suspended && memorial.Status == MemorialStatus.Published)
        {
            memorial.PublishedAt ??= paidAt;
            memorial.ArchivedAt = null;
        }

        // Draft stays Draft; Archive stays Archive (admin should restore first).
        await _memorials.ReplaceAsync(memorial, ct);
    }

    private static MemorialPaymentDto MapPayment(MemorialPayment p) => new()
    {
        Id = p.Id,
        MemorialId = p.MemorialId,
        CustomerId = p.CustomerId,
        Amount = p.Amount,
        PaidAt = p.PaidAt,
        PeriodFrom = p.PeriodFrom,
        PeriodTo = p.PeriodTo,
        Type = p.Type,
        Method = p.Method,
        Note = p.Note,
        CreatedBy = p.CreatedBy,
        CreatedAt = p.CreatedAt
    };
}
