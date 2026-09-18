using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;

namespace Nezabuti.Api.Services;

public static class HomeShowcaseMapper
{
    public const int MaxSlides = 12;
    public const string DefaultDemoTitle = "Подивіться, як виглядає готова сторінка";
    public const string DefaultDemoButtonLabel = "Відкрити демо-сторінку";

    public static bool IsAllowedDemoUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();
        if (trimmed.StartsWith('/') && !trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return trimmed.Length > 1 && !trimmed.Contains('\\');
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme is "http" or "https";
    }

    public static HomeShowcaseSettings Normalize(HomeShowcaseSettings? source)
    {
        var showcase = source ?? new HomeShowcaseSettings();
        var slides = (showcase.HowItWorksSlides ?? [])
            .Select((slide, index) => NormalizeSlide(slide, index))
            .Take(MaxSlides)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Title, StringComparer.Ordinal)
            .ToList();

        for (var i = 0; i < slides.Count; i++)
        {
            slides[i].SortOrder = i;
        }

        return new HomeShowcaseSettings
        {
            HowItWorksEnabled = showcase.HowItWorksEnabled,
            HowItWorksSlides = slides,
            DemoEnabled = showcase.DemoEnabled,
            DemoTitle = (showcase.DemoTitle ?? string.Empty).Trim(),
            DemoDescription = (showcase.DemoDescription ?? string.Empty).Trim(),
            DemoUrl = (showcase.DemoUrl ?? string.Empty).Trim(),
            DemoPreviewImage = showcase.DemoPreviewImage,
            SeedMobileRevision = showcase.SeedMobileRevision
        };
    }

    public static void ValidateForSave(HomeShowcaseSettings showcase)
    {
        if (showcase.DemoEnabled && !IsAllowedDemoUrl(showcase.DemoUrl))
        {
            throw new InvalidOperationException("Вкажіть коректне посилання на демо-сторінку (https://… або /m/…).");
        }
    }

    public static IReadOnlyList<string> CollectPhotoIds(HomeShowcaseSettings showcase)
    {
        var ids = new List<string>();
        foreach (var slide in showcase.HowItWorksSlides)
        {
            AddPhotoId(ids, ResolveDesktop(slide));
            AddPhotoId(ids, slide.MobileImage);
        }

        AddPhotoId(ids, showcase.DemoPreviewImage);
        return ids;
    }

    public static PhotoRef? ResolveDesktop(HowItWorksSlide slide) =>
        HasPhoto(slide.DesktopImage) ? slide.DesktopImage : slide.Image;

    public static PhotoRef? ResolveMobile(HowItWorksSlide slide) =>
        HasPhoto(slide.MobileImage) ? slide.MobileImage : ResolveDesktop(slide);

    public static List<HowItWorksSlideDto> ToAdminSlides(HomeShowcaseSettings showcase, Func<PhotoRef, PhotoRefDto> mapPhoto) =>
        showcase.HowItWorksSlides.Select(s =>
        {
            var desktop = ResolveDesktop(s);
            return new HowItWorksSlideDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                ShortCaption = s.ShortCaption,
                Image = desktop is null ? null : mapPhoto(desktop),
                DesktopImage = desktop is null ? null : mapPhoto(desktop),
                MobileImage = HasPhoto(s.MobileImage) ? mapPhoto(s.MobileImage!) : null,
                AltText = s.AltText,
                SortOrder = s.SortOrder,
                Enabled = s.Enabled
            };
        }).ToList();

    public static HomeShowcaseAdminDto ToAdmin(HomeShowcaseSettings showcase, Func<PhotoRef, PhotoRefDto> mapPhoto)
    {
        var normalized = Normalize(showcase);
        return new HomeShowcaseAdminDto
        {
            HowItWorksEnabled = normalized.HowItWorksEnabled,
            HowItWorksSlides = ToAdminSlides(normalized, mapPhoto),
            DemoEnabled = normalized.DemoEnabled,
            DemoTitle = normalized.DemoTitle,
            DemoDescription = normalized.DemoDescription,
            DemoUrl = normalized.DemoUrl,
            DemoPreviewImage = normalized.DemoPreviewImage is null ? null : mapPhoto(normalized.DemoPreviewImage)
        };
    }

    public static (bool HowItWorksEnabled, List<PublicHowItWorksSlideDto> Slides, PublicDemoCtaDto? Demo)
        ToPublic(HomeShowcaseSettings? showcase, Func<PhotoRef, PhotoRefDto> mapPhoto)
    {
        var normalized = Normalize(showcase);
        var slides = normalized.HowItWorksEnabled
            ? normalized.HowItWorksSlides
                .Where(s => s.Enabled && HasPhoto(ResolveDesktop(s)) && !string.IsNullOrWhiteSpace(s.Title))
                .Select(s =>
                {
                    var desktop = mapPhoto(ResolveDesktop(s)!);
                    var mobileRef = HasPhoto(s.MobileImage) ? mapPhoto(s.MobileImage!) : null;
                    return new PublicHowItWorksSlideDto
                    {
                        Title = s.Title,
                        Description = s.Description,
                        ShortCaption = string.IsNullOrWhiteSpace(s.ShortCaption) ? null : s.ShortCaption,
                        AltText = string.IsNullOrWhiteSpace(s.AltText) ? s.Title : s.AltText,
                        SortOrder = s.SortOrder,
                        Image = desktop,
                        DesktopImage = desktop,
                        MobileImage = mobileRef,
                        DesktopImageUrl = ToImageUrl(desktop),
                        MobileImageUrl = ToImageUrl(mobileRef) ?? ToImageUrl(desktop)
                    };
                })
                .ToList()
            : [];

        for (var i = 0; i < slides.Count; i++)
        {
            slides[i].SortOrder = i;
        }

        PublicDemoCtaDto? demo = null;
        if (normalized.DemoEnabled && IsAllowedDemoUrl(normalized.DemoUrl))
        {
            demo = new PublicDemoCtaDto
            {
                Title = string.IsNullOrWhiteSpace(normalized.DemoTitle) ? DefaultDemoTitle : normalized.DemoTitle,
                Description = normalized.DemoDescription,
                Url = normalized.DemoUrl.Trim(),
                ButtonLabel = DefaultDemoButtonLabel,
                PreviewImage = normalized.DemoPreviewImage is null ? null : mapPhoto(normalized.DemoPreviewImage)
            };
        }

        return (slides.Count > 0, slides, demo);
    }

    public static HomeShowcaseSettings FromAdminRequest(HomeShowcaseAdminDto? dto)
    {
        if (dto is null)
        {
            return new HomeShowcaseSettings();
        }

        return Normalize(new HomeShowcaseSettings
        {
            HowItWorksEnabled = dto.HowItWorksEnabled,
            HowItWorksSlides = (dto.HowItWorksSlides ?? []).Select(s =>
            {
                var desktop = FromPhotoDto(s.DesktopImage) ?? FromPhotoDto(s.Image);
                return new HowItWorksSlide
                {
                    Id = s.Id ?? string.Empty,
                    Title = s.Title,
                    Description = s.Description,
                    ShortCaption = s.ShortCaption,
                    Image = desktop,
                    DesktopImage = desktop,
                    MobileImage = FromPhotoDto(s.MobileImage),
                    AltText = s.AltText,
                    SortOrder = s.SortOrder,
                    Enabled = s.Enabled
                };
            }).ToList(),
            DemoEnabled = dto.DemoEnabled,
            DemoTitle = dto.DemoTitle,
            DemoDescription = dto.DemoDescription,
            DemoUrl = dto.DemoUrl,
            DemoPreviewImage = FromPhotoDto(dto.DemoPreviewImage)
        });
    }

    private static HowItWorksSlide NormalizeSlide(HowItWorksSlide? slide, int index)
    {
        slide ??= new HowItWorksSlide();
        var desktop = ResolveDesktop(slide);
        return new HowItWorksSlide
        {
            Id = string.IsNullOrWhiteSpace(slide.Id) ? Guid.NewGuid().ToString("N") : slide.Id.Trim(),
            Title = (slide.Title ?? string.Empty).Trim(),
            Description = (slide.Description ?? string.Empty).Trim(),
            ShortCaption = string.IsNullOrWhiteSpace(slide.ShortCaption) ? null : slide.ShortCaption.Trim(),
            Image = desktop,
            DesktopImage = desktop,
            MobileImage = HasPhoto(slide.MobileImage) ? slide.MobileImage : null,
            AltText = string.IsNullOrWhiteSpace(slide.AltText) ? null : slide.AltText.Trim(),
            SortOrder = slide.SortOrder == 0 && index > 0 ? index : slide.SortOrder,
            Enabled = slide.Enabled
        };
    }

    private static bool HasPhoto(PhotoRef? photo) =>
        photo is not null && !string.IsNullOrWhiteSpace(photo.PhotoId);

    private static void AddPhotoId(List<string> ids, PhotoRef? photo)
    {
        if (HasPhoto(photo))
        {
            ids.Add(photo!.PhotoId);
        }
    }

    public static string? ToImageUrl(PhotoRefDto? photo)
    {
        if (photo is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(photo.PreviewUrl))
        {
            return photo.PreviewUrl;
        }

        if (!string.IsNullOrWhiteSpace(photo.FullUrl))
        {
            return photo.FullUrl;
        }

        return string.IsNullOrWhiteSpace(photo.ThumbUrl) ? null : photo.ThumbUrl;
    }

    private static PhotoRef? FromPhotoDto(PhotoRefDto? dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.PhotoId))
        {
            return null;
        }

        return new PhotoRef
        {
            PhotoId = dto.PhotoId,
            ThumbPath = FromMediaUrl(dto.ThumbUrl),
            PreviewPath = FromMediaUrl(dto.PreviewUrl),
            FullPath = FromMediaUrl(dto.FullUrl),
            Width = dto.Width,
            Height = dto.Height
        };
    }

    private static string FromMediaUrl(string? url)
    {
        var value = (url ?? string.Empty).Trim();
        const string prefix = "/uploads/";
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return value[prefix.Length..];
        }

        return value.TrimStart('/');
    }
}
