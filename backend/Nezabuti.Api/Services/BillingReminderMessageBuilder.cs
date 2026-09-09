using Nezabuti.Api.Models;

namespace Nezabuti.Api.Services;

/// <summary>
/// Pure Telegram billing reminder text (unit-testable, no I/O).
/// </summary>
public static class BillingReminderMessageBuilder
{
    public static string Build(
        Memorial memorial,
        string eventKind,
        DateTime todayLocal,
        string adminMemorialUrl)
    {
        var paidUntil = memorial.PaidUntil?.ToString("dd.MM.yyyy") ?? "—";
        var remaining = memorial.PaidUntil is null
            ? "—"
            : FormatRemaining(BillingCalendar.DaysUntil(memorial.PaidUntil.Value, todayLocal));

        var headline = eventKind switch
        {
            "reminder30" => "Nezabuti — продовження меморіалу (30 днів)",
            "reminder7" => "Nezabuti — продовження меморіалу (7 днів)",
            "expired" => "Nezabuti — оплачений період завершено",
            "grace7" => "Nezabuti — пільговий період (7 днів)",
            "suspended" => "Nezabuti — сторінку призупинено",
            _ => "Nezabuti — продовження меморіалу"
        };

        var remainingLine = eventKind switch
        {
            "expired" => "Оплачений період завершився сьогодні.",
            "suspended" => "Сторінку автоматично призупинено після пільгового періоду.",
            "grace7" => memorial.GraceUntil is null
                ? "Пільговий період до: —"
                : $"Пільговий період до: {memorial.GraceUntil.Value:dd.MM.yyyy}\nЗалишилось: {FormatRemaining(BillingCalendar.DaysUntil(memorial.GraceUntil.Value, todayLocal))}",
            _ => $"Оплачено до: {paidUntil}\nЗалишилось: {remaining}"
        };

        return $"""
            {headline}

            {memorial.FullName}
            {remainingLine}

            Статус: {BillingCalendar.MemorialStatusLabelUk(memorial.Status)}

            Відкрити в адмінці:
            {adminMemorialUrl}
            """.Replace("\r\n", "\n").Trim();
    }

    private static string FormatRemaining(int days)
    {
        if (days < 0)
        {
            return "прострочено";
        }

        if (days == 0)
        {
            return "сьогодні";
        }

        var mod100 = days % 100;
        var mod10 = days % 10;
        var unit = mod100 is >= 11 and <= 14
            ? "днів"
            : mod10 == 1
                ? "день"
                : mod10 is >= 2 and <= 4
                    ? "дні"
                    : "днів";

        return $"{days} {unit}";
    }
}
