namespace Nezabuti.Api.Services;

public static class MemorialListSort
{
    public const string UpdatedAt = "updatedAt";
    public const string ViewCount = "viewCount";

    public static string NormalizeSortBy(string? sortBy) =>
        string.Equals(sortBy, ViewCount, StringComparison.OrdinalIgnoreCase) ? ViewCount : UpdatedAt;

    public static bool IsAscending(string? sortDir) =>
        string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
}
