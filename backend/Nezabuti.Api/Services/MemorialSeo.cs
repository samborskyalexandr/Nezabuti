using Nezabuti.Api.Models;

namespace Nezabuti.Api.Services;

/// <summary>
/// Public memorial robots meta. Demo pages are never indexed, even when Public + Published.
/// </summary>
public static class MemorialSeo
{
    public const string NoIndexNofollow = "noindex,nofollow";
    public const string IndexFollow = "index,follow";

    public static string Robots(bool isDemo, MemorialPrivacy privacy, bool forAdminPreview = false)
    {
        if (forAdminPreview || isDemo || privacy == MemorialPrivacy.Private)
        {
            return NoIndexNofollow;
        }

        return IndexFollow;
    }
}
