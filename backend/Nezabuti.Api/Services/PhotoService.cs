using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Nezabuti.Api.Configuration;
using Nezabuti.Api.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Nezabuti.Api.Services;

public interface IPhotoService
{
    Task<PhotoRef> ProcessUploadAsync(string publicId, Stream uploadStream, string contentType, CancellationToken ct = default);
    Task DeletePhotoFilesAsync(string publicId, string photoId, CancellationToken ct = default);
    Task DeleteMemorialDirectoryAsync(string publicId, CancellationToken ct = default);
    string GetAbsolutePath(string relativePath);
    bool IsSafeRelativePath(string relativePath);
}

public sealed class PhotoService : IPhotoService
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private readonly ImageSettings _settings;
    private readonly ILogger<PhotoService> _logger;

    public PhotoService(IOptions<ImageSettings> settings, ILogger<PhotoService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PhotoRef> ProcessUploadAsync(
        string publicId,
        Stream uploadStream,
        string contentType,
        CancellationToken ct = default)
    {
        ValidatePublicId(publicId);

        if (!AllowedMimeTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Непідтримуваний тип зображення.");
        }

        var memorialDir = Path.Combine(_settings.UploadsRoot, "memorials", publicId);
        Directory.CreateDirectory(memorialDir);

        var photoId = GenerateFileId();
        var tempPath = Path.Combine(memorialDir, $".tmp-{photoId}");

        var thumbRel = $"memorials/{publicId}/{photoId}-thumb.webp";
        var previewRel = $"memorials/{publicId}/{photoId}-preview.webp";
        var fullRel = $"memorials/{publicId}/{photoId}-full.webp";

        var thumbAbs = GetAbsolutePath(thumbRel);
        var previewAbs = GetAbsolutePath(previewRel);
        var fullAbs = GetAbsolutePath(fullRel);

        try
        {
            await using (var temp = File.Create(tempPath))
            {
                await uploadStream.CopyToAsync(temp, ct);
            }

            var length = new FileInfo(tempPath).Length;
            if (length <= 0)
            {
                throw new InvalidOperationException("Потрібен файл зображення.");
            }

            if (length > _settings.MaxUploadBytes)
            {
                var maxMb = Math.Max(1, _settings.MaxUploadBytes / (1024 * 1024));
                throw new InvalidOperationException($"Максимальний розмір фото — {maxMb} МБ.");
            }

            await using var input = File.OpenRead(tempPath);
            using var image = await Image.LoadAsync(input, ct);

            image.Mutate(x => x.AutoOrient());
            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.XmpProfile = null;

            var originalWidth = image.Width;
            var originalHeight = image.Height;
            var encoder = new WebpEncoder { Quality = _settings.WebpQuality };

            using (var fullImage = image.Clone(ctx => ResizeIfNeeded(ctx, _settings.FullMaxDimension)))
            {
                await fullImage.SaveAsync(fullAbs, encoder, ct);
            }

            using (var previewImage = image.Clone(ctx => ResizeIfNeeded(ctx, _settings.PreviewMaxDimension)))
            {
                await previewImage.SaveAsync(previewAbs, encoder, ct);
            }

            using (var thumbImage = image.Clone(ctx => ResizeIfNeeded(ctx, _settings.ThumbMaxDimension)))
            {
                await thumbImage.SaveAsync(thumbAbs, encoder, ct);
            }

            return new PhotoRef
            {
                PhotoId = photoId,
                ThumbPath = thumbRel.Replace('\\', '/'),
                PreviewPath = previewRel.Replace('\\', '/'),
                FullPath = fullRel.Replace('\\', '/'),
                Width = originalWidth,
                Height = originalHeight
            };
        }
        catch
        {
            TryDelete(thumbAbs);
            TryDelete(previewAbs);
            TryDelete(fullAbs);
            throw;
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    public Task DeletePhotoFilesAsync(string publicId, string photoId, CancellationToken ct = default)
    {
        ValidatePublicId(publicId);
        ValidatePhotoId(photoId);

        TryDelete(GetAbsolutePath($"memorials/{publicId}/{photoId}-thumb.webp"));
        TryDelete(GetAbsolutePath($"memorials/{publicId}/{photoId}-preview.webp"));
        TryDelete(GetAbsolutePath($"memorials/{publicId}/{photoId}-full.webp"));
        return Task.CompletedTask;
    }

    public Task DeleteMemorialDirectoryAsync(string publicId, CancellationToken ct = default)
    {
        ValidatePublicId(publicId);
        var dir = Path.Combine(_settings.UploadsRoot, "memorials", publicId);
        var fullRoot = Path.GetFullPath(_settings.UploadsRoot);
        var fullDir = Path.GetFullPath(dir);

        if (!fullDir.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid path.");
        }

        if (Directory.Exists(fullDir))
        {
            Directory.Delete(fullDir, recursive: true);
        }

        return Task.CompletedTask;
    }

    public string GetAbsolutePath(string relativePath)
    {
        if (!IsSafeRelativePath(relativePath))
        {
            throw new InvalidOperationException("Unsafe path.");
        }

        var combined = Path.GetFullPath(Path.Combine(_settings.UploadsRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(_settings.UploadsRoot);
        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path traversal detected.");
        }

        return combined;
    }

    public bool IsSafeRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var normalized = relativePath.Replace('\\', '/');
        if (normalized.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(normalized) ||
            normalized.StartsWith('/'))
        {
            return false;
        }

        return normalized.StartsWith("memorials/", StringComparison.OrdinalIgnoreCase);
    }

    private static void ResizeIfNeeded(IImageProcessingContext ctx, int maxDimension)
    {
        var size = ctx.GetCurrentSize();
        var longSide = Math.Max(size.Width, size.Height);
        if (longSide <= maxDimension)
        {
            return;
        }

        ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = size.Width >= size.Height
                ? new Size(maxDimension, 0)
                : new Size(0, maxDimension)
        });
    }

    private static string GenerateFileId()
    {
        var bytes = RandomNumberGenerator.GetBytes(12);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static void ValidatePublicId(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId) ||
            publicId.Contains('.') ||
            publicId.Contains('/') ||
            publicId.Contains('\\'))
        {
            throw new InvalidOperationException("Invalid PublicId for photo path.");
        }
    }

    private static void ValidatePhotoId(string photoId)
    {
        if (string.IsNullOrWhiteSpace(photoId) ||
            photoId.Contains('.') ||
            photoId.Contains('/') ||
            photoId.Contains('\\'))
        {
            throw new InvalidOperationException("Invalid photo id.");
        }
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp/partial file {Path}", path);
        }
    }
}
