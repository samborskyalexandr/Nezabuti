namespace Nezabuti.Api.Models;

public enum MemorialStatus
{
    Draft = 0,
    Published = 1,
    /// <summary>Soft-delete / admin archive — not a billing status.</summary>
    Archived = 2,
    /// <summary>Auto-paused after grace period for unpaid published pages.</summary>
    Suspended = 3
}
