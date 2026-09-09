/** Deterministic memorial fixtures for billing E2E (no production IDs). */

export const MEMORIAL_ID = '507f1f77bcf86cd799439011';

export function baseMemorial(overrides: Record<string, unknown> = {}) {
  return {
    id: MEMORIAL_ID,
    publicId: 'E2ETEST001',
    publicUrl: 'http://127.0.0.1:4200/m/E2ETEST001',
    fullName: 'E2E Тестовий',
    mainPhoto: null,
    status: 'Published',
    privacy: 'Public',
    isDemo: false,
    customerId: null,
    customer: null,
    blocks: [],
    callsign: null,
    lifePeriod: '1990–2024',
    shortText: 'Тест',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-02T00:00:00Z',
    publishedAt: '2026-01-01T00:00:00Z',
    archivedAt: null,
    planSnapshot: {
      planId: '507f1f77bcf86cd799439099',
      code: 'memory',
      name: "Пам'ять",
      price: 700,
      initialPrice: 700,
      renewalPrice: 300,
      maxBlocks: 4,
      maxGalleryBlocks: 1,
      maxPhotosPerGallery: 10,
      maxTimelineEvents: 5,
      maxMemories: 2,
      includedUpdates: 1,
      snapshotAt: '2026-01-01T00:00:00Z'
    },
    usedUpdates: 0,
    qrPlateSize: 'Size50',
    qrPriceDeltaSnapshot: 0,
    calculatedPrice: 700,
    finalPrice: 700,
    isFinalPriceOverridden: false,
    paymentStatus: 'Paid',
    paidAt: '2026-09-08T00:00:00Z',
    lastPaymentAt: '2026-09-08T00:00:00Z',
    // Far future → PeriodFrom = PaidUntil (deterministic vs browser "today")
    paidUntil: '2099-09-08T00:00:00Z',
    graceUntil: '2099-10-08T00:00:00Z',
    paymentState: 'Paid',
    paymentStateLabel: 'Оплачено',
    ...overrides
  };
}

export function renewedMemorial(status: string) {
  return baseMemorial({
    status,
    paidUntil: '2100-09-08T00:00:00Z',
    graceUntil: '2100-10-08T00:00:00Z',
    paymentState: 'Paid',
    paymentStateLabel: 'Оплачено',
    lastPaymentAt: '2026-09-09T10:00:00Z'
  });
}

export const renewalPayment = {
  id: '507f1f77bcf86cd7994390aa',
  memorialId: MEMORIAL_ID,
  customerId: null,
  amount: 300,
  paidAt: '2026-09-09T10:00:00Z',
  periodFrom: '2099-09-08T00:00:00Z',
  periodTo: '2100-09-08T00:00:00Z',
  type: 'Renewal',
  method: 'Manual',
  note: null,
  createdBy: 'admin',
  createdAt: '2026-09-09T10:00:00Z'
};
