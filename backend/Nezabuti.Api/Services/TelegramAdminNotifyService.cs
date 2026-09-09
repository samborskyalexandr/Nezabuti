using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Nezabuti.Api.Configuration;
using Nezabuti.Api.Models;
using Nezabuti.Api.Repositories;

namespace Nezabuti.Api.Services;

public interface ITelegramAdminNotifyService
{
    Task<(bool Ok, string Message)> SendMessageAsync(string text, CancellationToken ct = default);
    Task<(bool Ok, string Message)> SendTestMessageAsync(CancellationToken ct = default);
    Task<(bool Ok, string Message)> SendBillingReminderAsync(
        Memorial memorial,
        string eventKind,
        CancellationToken ct = default);
}

public sealed class TelegramAdminNotifyService : ITelegramAdminNotifyService
{
    /// <summary>Memorial used for admin "send test message" — real billing reminder format.</summary>
    public const string TestMemorialPublicId = "YSQG27AFFT";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISiteSettingsRepository _settings;
    private readonly IMemorialRepository _memorials;
    private readonly ISecretEncryptionService _secrets;
    private readonly AppPublicSettings _app;
    private readonly IBillingClock _clock;
    private readonly ILogger<TelegramAdminNotifyService> _logger;

    public TelegramAdminNotifyService(
        IHttpClientFactory httpClientFactory,
        ISiteSettingsRepository settings,
        IMemorialRepository memorials,
        ISecretEncryptionService secrets,
        IOptions<AppPublicSettings> app,
        IBillingClock clock,
        ILogger<TelegramAdminNotifyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _memorials = memorials;
        _secrets = secrets;
        _app = app.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<(bool Ok, string Message)> SendTestMessageAsync(CancellationToken ct = default)
    {
        var memorial = await _memorials.GetByPublicIdAsync(TestMemorialPublicId, ct);
        if (memorial is null)
        {
            return (false, $"Тестовий меморіал {TestMemorialPublicId} не знайдено.");
        }

        var kind = ResolveSampleReminderKind(memorial, _clock.TodayLocal);
        var (ok, message) = await SendBillingReminderAsync(memorial, kind, ct);
        if (!ok)
        {
            return (ok, message);
        }

        return (true, $"Надіслано зразок нагадування ({kind}) для {TestMemorialPublicId}.");
    }

    public async Task<(bool Ok, string Message)> SendBillingReminderAsync(
        Memorial memorial,
        string eventKind,
        CancellationToken ct = default)
    {
        var text = BuildBillingMessage(memorial, eventKind);
        return await SendMessageAsync(text, ct);
    }

    public async Task<(bool Ok, string Message)> SendMessageAsync(string text, CancellationToken ct = default)
    {
        var settings = await _settings.GetAsync(ct);
        if (!settings.TelegramNotifyEnabled)
        {
            return (false, "Telegram-сповіщення вимкнено.");
        }

        var token = _secrets.Decrypt(settings.TelegramBotTokenEnc);
        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, "Токен Telegram-бота не налаштовано.");
        }

        if (string.IsNullOrWhiteSpace(settings.TelegramChatId))
        {
            return (false, "Chat ID не налаштовано.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(TelegramAdminNotifyService));
            var url = $"https://api.telegram.org/bot{token}/sendMessage";
            using var response = await client.PostAsJsonAsync(
                url,
                new TelegramSendRequest
                {
                    ChatId = settings.TelegramChatId.Trim(),
                    Text = text,
                    DisableWebPagePreview = true
                },
                ct);

            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Telegram sendMessage failed: {Status} {Body}", (int)response.StatusCode, body);
                return (false, $"Помилка Telegram API ({(int)response.StatusCode}).");
            }

            return (true, "Повідомлення надіслано.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram sendMessage exception");
            return (false, "Не вдалося надіслати повідомлення в Telegram.");
        }
    }

    private string BuildBillingMessage(Memorial memorial, string eventKind)
    {
        var adminUrl = BuildAdminMemorialUrl(memorial.Id);
        return BillingReminderMessageBuilder.Build(memorial, eventKind, _clock.TodayLocal, adminUrl);
    }

    private string BuildAdminMemorialUrl(string memorialId)
    {
        var baseUrl = _app.PublicBaseUrl.TrimEnd('/');
        var adminPath = (_app.AdminBasePath ?? string.Empty).Trim('/');
        return $"{baseUrl}/{adminPath}/memorials/{memorialId}";
    }

    /// <summary>
    /// Pick reminder template that matches the memorial's current billing calendar state
    /// so the test message looks like a real admin notification.
    /// </summary>
    public static string ResolveSampleReminderKind(Memorial memorial, DateTime todayLocal)
    {
        if (memorial.Status == MemorialStatus.Suspended)
        {
            return "suspended";
        }

        if (memorial.PaidUntil is null || memorial.GraceUntil is null)
        {
            return "reminder30";
        }

        var today = todayLocal.Date;
        var paid = memorial.PaidUntil.Value.Date;
        var grace = memorial.GraceUntil.Value.Date;
        var daysToPaid = BillingCalendar.DaysUntil(paid, today);

        if (today > grace)
        {
            return "suspended";
        }

        if (today > paid && today <= grace)
        {
            return "grace7";
        }

        if (daysToPaid == 0)
        {
            return "expired";
        }

        if (daysToPaid is >= 1 and <= 7)
        {
            return "reminder7";
        }

        return "reminder30";
    }

    private sealed class TelegramSendRequest
    {
        [JsonPropertyName("chat_id")]
        public string ChatId { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("disable_web_page_preview")]
        public bool DisableWebPagePreview { get; set; }
    }
}
