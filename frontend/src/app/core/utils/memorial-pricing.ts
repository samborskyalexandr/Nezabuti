import { PlanSnapshot, QrPlateSize, SiteSettings } from '../models/memorial.models';

export function qrPriceDeltaFromSettings(settings: SiteSettings | null | undefined, size: QrPlateSize): number {
  if (!settings) {
    return 0;
  }
  switch (size) {
    case 'Size75':
      return settings.qrSize75PriceDelta;
    case 'Size100':
      return settings.qrSize100PriceDelta;
    case 'Size50':
    default:
      return settings.qrSize50PriceDelta;
  }
}

export function calculateMemorialPrice(planPrice: number, qrDelta: number): number {
  return planPrice + qrDelta;
}

export function resolveQrDelta(
  memorialQrDeltaSnapshot: number | null | undefined,
  settings: SiteSettings | null | undefined,
  size: QrPlateSize
): number {
  if (memorialQrDeltaSnapshot != null) {
    return memorialQrDeltaSnapshot;
  }
  return qrPriceDeltaFromSettings(settings, size);
}

export function calculatedPriceFor(
  snapshot: PlanSnapshot | null | undefined,
  qrDelta: number
): number | null {
  if (!snapshot) {
    return null;
  }
  return calculateMemorialPrice(snapshot.price, qrDelta);
}
