namespace Nezabuti.Api.Models;

/// <summary>
/// Computed billing state from PaidUntil / GraceUntil (not stored).
/// </summary>
public enum PaymentState
{
    /// <summary>Billing dates not configured (legacy / unset).</summary>
    Unconfigured = 0,
    Paid = 1,
    Grace = 2,
    Expired = 3
}
