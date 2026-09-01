using System.Text.RegularExpressions;
using MongoDB.Bson;
using Nezabuti.Api.Models;
using Nezabuti.Api.Models.Blocks;

namespace Nezabuti.Api.Services;

public static class ContentLimitValidator
{
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);

    public static (bool Ok, string? Error) Validate(Memorial memorial, SiteSettings settings)
    {
        if (!string.IsNullOrEmpty(memorial.ShortText)
            && PlainLength(memorial.ShortText) > settings.ShortTextMaxChars)
        {
            return (false, $"Короткий опис перевищує ліміт {settings.ShortTextMaxChars} символів.");
        }

        foreach (var block in memorial.Blocks)
        {
            var result = ValidateBlock(block, settings);
            if (!result.Ok)
            {
                return result;
            }
        }

        return (true, null);
    }

    private static (bool Ok, string? Error) ValidateBlock(MemorialBlock block, SiteSettings settings)
    {
        return block.Type switch
        {
            BlockType.Text => ValidateText(block, settings.TextBlockMaxChars),
            BlockType.Quote => ValidateField(block.Data, "text", settings.QuoteMaxChars, "Цитата"),
            BlockType.Timeline => ValidateTimeline(block, settings),
            BlockType.Gallery => ValidateGalleryCaptions(block, settings),
            BlockType.Image => ValidateField(block.Data, "caption", settings.PhotoCaptionMaxChars, "Підпис фото"),
            BlockType.Service => ValidateField(block.Data, "description", settings.ServiceDescriptionMaxChars, "Опис служби"),
            BlockType.Awards => ValidateAwards(block, settings),
            BlockType.Memories => ValidateMemories(block, settings),
            _ => (true, null)
        };
    }

    private static (bool Ok, string? Error) ValidateText(MemorialBlock block, int max)
    {
        if (!block.Data.Contains("html"))
        {
            return (true, null);
        }

        var len = PlainLength(block.Data["html"].AsString);
        if (len > max)
        {
            return (false, $"Текстовий блок перевищує ліміт {max} символів.");
        }

        return (true, null);
    }

    private static (bool Ok, string? Error) ValidateField(BsonDocument data, string key, int max, string label)
    {
        if (!data.Contains(key))
        {
            return (true, null);
        }

        var len = PlainLength(data[key].ToString());
        if (len > max)
        {
            return (false, $"{label} перевищує ліміт {max} символів.");
        }

        return (true, null);
    }

    private static (bool Ok, string? Error) ValidateTimeline(MemorialBlock block, SiteSettings settings)
    {
        if (!block.Data.TryGetValue("events", out var events) || events is not BsonArray arr)
        {
            return (true, null);
        }

        foreach (var item in arr)
        {
            if (item is not BsonDocument doc)
            {
                continue;
            }

            if (doc.Contains("description")
                && PlainLength(doc["description"].ToString()) > settings.TimelineDescriptionMaxChars)
            {
                return (false, $"Опис події життєвого шляху перевищує ліміт {settings.TimelineDescriptionMaxChars} символів.");
            }
        }

        return (true, null);
    }

    private static (bool Ok, string? Error) ValidateGalleryCaptions(MemorialBlock block, SiteSettings settings)
    {
        if (!block.Data.TryGetValue("items", out var items) || items is not BsonArray arr)
        {
            return (true, null);
        }

        foreach (var item in arr)
        {
            if (item is not BsonDocument doc || !doc.Contains("caption"))
            {
                continue;
            }

            if (PlainLength(doc["caption"].ToString()) > settings.PhotoCaptionMaxChars)
            {
                return (false, $"Підпис у галереї перевищує ліміт {settings.PhotoCaptionMaxChars} символів.");
            }
        }

        return (true, null);
    }

    private static (bool Ok, string? Error) ValidateAwards(MemorialBlock block, SiteSettings settings)
    {
        if (!block.Data.TryGetValue("items", out var items) || items is not BsonArray arr)
        {
            return (true, null);
        }

        foreach (var item in arr)
        {
            if (item is not BsonDocument doc || !doc.Contains("description"))
            {
                continue;
            }

            if (PlainLength(doc["description"].ToString()) > settings.AwardDescriptionMaxChars)
            {
                return (false, $"Опис відзнаки перевищує ліміт {settings.AwardDescriptionMaxChars} символів.");
            }
        }

        return (true, null);
    }

    private static (bool Ok, string? Error) ValidateMemories(MemorialBlock block, SiteSettings settings)
    {
        if (!block.Data.TryGetValue("items", out var items) || items is not BsonArray arr)
        {
            return (true, null);
        }

        foreach (var item in arr)
        {
            if (item is not BsonDocument doc || !doc.Contains("text"))
            {
                continue;
            }

            if (PlainLength(doc["text"].ToString()) > settings.MemoryTextMaxChars)
            {
                return (false, $"Текст спогаду перевищує ліміт {settings.MemoryTextMaxChars} символів.");
            }
        }

        return (true, null);
    }

    private static int PlainLength(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var plain = HtmlTagRegex.Replace(value, string.Empty)
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Trim();
        return plain.Length;
    }
}
