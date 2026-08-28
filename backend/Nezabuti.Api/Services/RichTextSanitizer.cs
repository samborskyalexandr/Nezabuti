using Ganss.Xss;

namespace Nezabuti.Api.Services;

public interface IRichTextSanitizer
{
    string Sanitize(string? html);
    string SanitizePlain(string? text);
}

/// <summary>
/// Allows only semantic formatting tags — no fonts, colors, or inline styles.
/// </summary>
public sealed class RichTextSanitizer : IRichTextSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public RichTextSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "p", "h2", "h3", "strong", "em", "b", "i",
                     "ul", "ol", "li", "blockquote", "br"
                 })
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowDataAttributes = false;
    }

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return _sanitizer.Sanitize(html);
    }

    public string SanitizePlain(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return System.Net.WebUtility.HtmlEncode(text.Trim());
    }
}
