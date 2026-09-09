using Nezabuti.Api.Models;

namespace Nezabuti.Api.Services;

/// <summary>
/// Admin bookkeeping helpers for memorial price calculation (no payment gateway).
/// </summary>
public static class MemorialPricing
{
    public static decimal GetQrPriceDelta(SiteSettings settings, QrPlateSize size) => size switch
    {
        QrPlateSize.Size75 => settings.QrSize75PriceDelta,
        QrPlateSize.Size100 => settings.QrSize100PriceDelta,
        _ => settings.QrSize50PriceDelta
    };

    public static decimal CalculatePrice(decimal planPrice, decimal qrPriceDelta) =>
        planPrice + qrPriceDelta;

    public static decimal? ResolveFinalPrice(Memorial memorial)
    {
        if (memorial.FinalPrice.HasValue)
        {
            return memorial.FinalPrice;
        }

        if (memorial.PlanSnapshot is null)
        {
            return null;
        }

        return CalculatePrice(memorial.PlanSnapshot.ResolveInitialPrice(), memorial.QrPriceDeltaSnapshot);
    }

    public static decimal? ResolveCalculatedPrice(Memorial memorial)
    {
        if (memorial.PlanSnapshot is null)
        {
            return null;
        }

        return CalculatePrice(memorial.PlanSnapshot.ResolveInitialPrice(), memorial.QrPriceDeltaSnapshot);
    }

    public static decimal ResolveDefaultInitialAmount(Memorial memorial)
    {
        if (memorial.FinalPrice.HasValue)
        {
            return memorial.FinalPrice.Value;
        }

        var initial = memorial.PlanSnapshot?.ResolveInitialPrice() ?? 0m;
        return CalculatePrice(initial, memorial.QrPriceDeltaSnapshot);
    }

    public static decimal ResolveDefaultRenewalAmount(Memorial memorial) =>
        memorial.PlanSnapshot?.ResolveRenewalPrice() ?? 0m;
}
