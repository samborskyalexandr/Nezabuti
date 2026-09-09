using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Nezabuti.Api.Configuration;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Models.Blocks;
using Nezabuti.Api.Repositories;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Tests;

internal sealed class FakeBillingClock : IBillingClock
{
    public DateTime UtcNow { get; set; } = new(2026, 9, 9, 7, 0, 0, DateTimeKind.Utc);
    public DateTime TodayLocal { get; set; } = new(2026, 9, 9);
    public TimeZoneInfo TimeZone { get; set; } = BillingTimeZone.Resolve("Europe/Kyiv");
    public int DailyRunHour { get; set; } = 10;
}

internal sealed class FakeMemorialRepository : IMemorialRepository
{
    private readonly Dictionary<string, Memorial> _items = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, Memorial> Items => _items;

    public void Seed(Memorial memorial) => _items[memorial.Id] = memorial;

    public Task<Memorial?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_items.TryGetValue(id, out var m) ? m : null);

    public Task<Memorial?> GetByPublicIdAsync(string publicId, CancellationToken ct = default) =>
        Task.FromResult(_items.Values.FirstOrDefault(m =>
            string.Equals(m.PublicId, publicId, StringComparison.OrdinalIgnoreCase)));

    public Task ReplaceAsync(Memorial memorial, CancellationToken ct = default)
    {
        _items[memorial.Id] = memorial;
        return Task.CompletedTask;
    }

    public Task<List<Memorial>> ListBillingCandidatesAsync(CancellationToken ct = default) =>
        Task.FromResult(_items.Values
            .Where(m => !m.IsDemo
                        && m.Status != MemorialStatus.Archived
                        && m.PaidUntil is not null
                        && m.GraceUntil is not null)
            .ToList());

    public Task<Memorial> CreateAsync(CreateMemorialRequest request, PlanSnapshot snapshot, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<(List<Memorial> Items, long Total)> ListAsync(
        string? search, MemorialStatus? status, bool? isDemo, BillingFilter? billingFilter,
        int page, int pageSize, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<Memorial?> UpdateAsync(string id, UpdateMemorialRequest request, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<Memorial?> UpdatePlanSnapshotAsync(string id, PlanSnapshot snapshot, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<Memorial?> AdjustUsedUpdatesAsync(string id, int delta, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<Memorial?> UpdatePaymentAsync(string id, PaymentStatus status, DateTime? paidAt, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<Memorial?> ReorderBlocksAsync(string id, IReadOnlyList<string> blockIds, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<Memorial?> SetStatusAsync(string id, MemorialStatus status, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<bool> DeleteArchivedAsync(string id, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<bool> PublicIdExistsAsync(string publicId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task UpdateMainPhotoAsync(string id, PhotoRef? photo, CancellationToken ct = default) =>
        throw new NotSupportedException();
}

internal sealed class FakeMemorialPaymentRepository : IMemorialPaymentRepository
{
    public List<MemorialPayment> Payments { get; } = [];

    public Task<MemorialPayment> CreateAsync(MemorialPayment payment, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(payment.Id))
        {
            payment.Id = ObjectId.GenerateNewId().ToString();
        }

        payment.CreatedAt = payment.CreatedAt == default ? DateTime.UtcNow : payment.CreatedAt;
        Payments.Add(payment);
        return Task.FromResult(payment);
    }

    public Task<List<MemorialPayment>> ListByMemorialIdAsync(string memorialId, CancellationToken ct = default) =>
        Task.FromResult(Payments.Where(p => p.MemorialId == memorialId).OrderByDescending(p => p.PaidAt).ToList());
}

internal sealed class FakeTelegramAdminNotifyService : ITelegramAdminNotifyService
{
    public List<(Memorial Memorial, string Kind)> BillingCalls { get; } = [];
    public bool Enabled { get; set; } = true;
    public bool ThrowOnSend { get; set; }
    public Exception? ExceptionToThrow { get; set; }

    public Task<(bool Ok, string Message)> SendMessageAsync(string text, CancellationToken ct = default) =>
        Task.FromResult(Enabled ? (true, "ok") : (false, "disabled"));

    public Task<(bool Ok, string Message)> SendTestMessageAsync(CancellationToken ct = default) =>
        SendMessageAsync("test", ct);

    public Task<(bool Ok, string Message)> SendBillingReminderAsync(
        Memorial memorial,
        string eventKind,
        CancellationToken ct = default)
    {
        if (ThrowOnSend)
        {
            throw ExceptionToThrow ?? new InvalidOperationException("telegram boom");
        }

        BillingCalls.Add((memorial, eventKind));
        return Task.FromResult(Enabled ? (true, "ok") : (false, "Telegram-сповіщення вимкнено."));
    }
}

internal sealed class StubPhotoService : IPhotoService
{
    public Task DeletePhotoFilesAsync(string publicId, string photoId, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteMemorialDirectoryAsync(string publicId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<PhotoRef> ProcessUploadAsync(string publicId, Stream uploadStream, string contentType, CancellationToken ct = default) =>
        throw new NotSupportedException();
    public string GetAbsolutePath(string relativePath) => relativePath;
    public bool IsSafeRelativePath(string relativePath) => true;
}

internal sealed class StubStatisticsService : IStatisticsService
{
    public Task EnsureExistsAsync(Memorial memorial, CancellationToken ct = default) => Task.CompletedTask;
    public Task RecordPublicViewAsync(string publicId, bool isAdminPreview, CancellationToken ct = default) => Task.CompletedTask;
    public Task<MemorialStatisticsDto?> GetAsync(string publicId, CancellationToken ct = default) =>
        Task.FromResult<MemorialStatisticsDto?>(new MemorialStatisticsDto { PublicId = publicId });
    public Task DeleteByMemorialIdAsync(string memorialId, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class StubPlanRepository : IPlanRepository
{
    public Task EnsureBootstrapAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<Plan>> ListAsync(bool activeOnly = false, CancellationToken ct = default) => Task.FromResult(new List<Plan>());
    public Task<Plan?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<Plan?>(null);
    public Task<Plan?> GetByCodeAsync(string code, CancellationToken ct = default) => Task.FromResult<Plan?>(null);
    public Task<Plan?> UpdateAsync(Plan plan, CancellationToken ct = default) => Task.FromResult<Plan?>(plan);
}

internal sealed class StubCustomerRepository : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<Customer?>(null);
    public Task<List<Customer>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default) =>
        Task.FromResult(new List<Customer>());
    public Task<(List<Customer> Items, long Total)> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default) =>
        Task.FromResult((new List<Customer>(), 0L));
    public Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default) => Task.FromResult(customer);
    public Task<Customer?> UpdateAsync(Customer customer, CancellationToken ct = default) => Task.FromResult<Customer?>(customer);
    public Task<bool> DeleteAsync(string id, CancellationToken ct = default) => Task.FromResult(false);
}

internal sealed class StubSiteSettingsRepository : ISiteSettingsRepository
{
    public SiteSettings Settings { get; set; } = new();

    public Task<SiteSettings> GetAsync(CancellationToken ct = default) => Task.FromResult(Settings);
    public Task<SiteSettings> UpsertAsync(SiteSettings settings, CancellationToken ct = default)
    {
        Settings = settings;
        return Task.FromResult(settings);
    }
}

internal static class BillingTestFixtures
{
    public static string NewId() => ObjectId.GenerateNewId().ToString();

    public static Memorial CreateMemorial(
        MemorialStatus status = MemorialStatus.Published,
        DateTime? paidUntil = null,
        DateTime? graceUntil = null,
        bool isDemo = false,
        decimal renewalPrice = 300m,
        decimal initialPrice = 700m,
        decimal? finalPrice = 700m)
    {
        var id = NewId();
        return new Memorial
        {
            Id = id,
            PublicId = "T" + id[^8..].ToUpperInvariant(),
            FullName = "Тестовий Меморіал",
            Status = status,
            Privacy = MemorialPrivacy.Public,
            IsDemo = isDemo,
            PaidUntil = paidUntil,
            GraceUntil = graceUntil,
            FinalPrice = finalPrice,
            PaymentStatus = paidUntil is null ? PaymentStatus.Unpaid : PaymentStatus.Paid,
            PlanSnapshot = new PlanSnapshot
            {
                PlanId = NewId(),
                Code = "memory",
                Name = "Пам'ять",
                Price = initialPrice,
                InitialPrice = initialPrice,
                RenewalPrice = renewalPrice,
                MaxBlocks = 4,
                IncludedUpdates = 1,
                SnapshotAt = DateTime.UtcNow
            },
            Reminder30SentAt = null,
            Reminder7SentAt = null,
            ExpiredReminderSentAt = null,
            Grace7ReminderSentAt = null,
            SuspendedReminderSentAt = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static MemorialBillingService CreateBillingService(
        FakeMemorialRepository memorials,
        FakeMemorialPaymentRepository payments,
        FakeBillingClock clock) =>
        new(memorials, payments, clock);

    public static MemorialBillingJobService CreateJobService(
        FakeMemorialRepository memorials,
        ITelegramAdminNotifyService telegram,
        FakeBillingClock clock) =>
        new(memorials, telegram, clock, NullLogger<MemorialBillingJobService>.Instance);

    public static MemorialService CreateMemorialService(FakeMemorialRepository memorials, FakeBillingClock? clock = null)
    {
        clock ??= new FakeBillingClock();
        return new MemorialService(
            memorials,
            new StubPhotoService(),
            new StubStatisticsService(),
            new StubPlanRepository(),
            new PlanLimitService(),
            new StubSiteSettingsRepository(),
            new StubCustomerRepository(),
            clock,
            Options.Create(new AppPublicSettings
            {
                PublicBaseUrl = "http://localhost:8088",
                AdminBasePath = "manage-nz7k4p"
            }));
    }
}
