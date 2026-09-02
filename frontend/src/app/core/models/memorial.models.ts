export type MemorialStatus = 'Draft' | 'Published' | 'Archived';
export type MemorialPrivacy = 'Public' | 'Private';
export type QrPlateSize = 'Size50' | 'Size75' | 'Size100';
export type PaymentStatus = 'Unpaid' | 'Paid';

export interface PhotoRef {
  photoId: string;
  thumbUrl: string;
  previewUrl: string;
  fullUrl: string;
  width?: number | null;
  height?: number | null;
}

export interface MemorialBlock {
  id?: string;
  type: string;
  order: number;
  data: Record<string, unknown>;
}

export interface SeoMeta {
  title: string;
  description: string;
  canonicalUrl: string;
  ogImageUrl?: string | null;
  robots: string;
}

export interface PublicMemorial {
  publicId: string;
  fullName: string;
  mainPhoto?: PhotoRef | null;
  privacy: MemorialPrivacy;
  isDemo?: boolean;
  blocks: MemorialBlock[];
  callsign?: string | null;
  lifePeriod?: string | null;
  shortText?: string | null;
  publishedAt?: string | null;
  seo: SeoMeta;
}

export interface PlanSnapshot {
  planId: string;
  code: string;
  name: string;
  price: number;
  isCustom: boolean;
  isUnlimited: boolean;
  maxBlocks?: number | null;
  maxGalleryBlocks?: number | null;
  maxPhotosPerGallery?: number | null;
  maxTimelineEvents?: number | null;
  maxMemories?: number | null;
  includedUpdates: number;
  snapshotAt: string;
}

export interface GalleryUsage {
  blockId: string;
  photosUsed: number;
  maxPhotosPerGallery?: number | null;
}

export interface PlanUsage {
  blocksUsed: number;
  maxBlocks?: number | null;
  galleriesUsed: number;
  maxGalleryBlocks?: number | null;
  timelineEventsUsed: number;
  maxTimelineEvents?: number | null;
  memoriesUsed: number;
  maxMemories?: number | null;
  usedUpdates: number;
  includedUpdates: number;
  isUnlimited: boolean;
  galleries: GalleryUsage[];
}

export interface Plan {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  price: number;
  isActive: boolean;
  isCustom: boolean;
  isUnlimited: boolean;
  maxBlocks?: number | null;
  maxGalleryBlocks?: number | null;
  maxPhotosPerGallery?: number | null;
  maxTimelineEvents?: number | null;
  maxMemories?: number | null;
  includedUpdates: number;
}

export interface PublicPlan {
  code: string;
  name: string;
  description?: string | null;
  price: number;
  maxGalleryBlocks?: number | null;
  maxPhotosPerGallery?: number | null;
  maxTimelineEvents?: number | null;
  maxMemories?: number | null;
  includedUpdates: number;
  isRecommended: boolean;
}

export interface CustomPlanOverrides {
  price?: number | null;
  isUnlimited?: boolean | null;
  maxBlocks?: number | null;
  maxGalleryBlocks?: number | null;
  maxPhotosPerGallery?: number | null;
  maxTimelineEvents?: number | null;
  maxMemories?: number | null;
  includedUpdates?: number | null;
}

export interface MemorialListItem {
  id: string;
  publicId: string;
  fullName: string;
  status: MemorialStatus;
  privacy: MemorialPrivacy;
  isDemo?: boolean;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string | null;
  archivedAt?: string | null;
  mainPhotoPreviewUrl?: string | null;
  mainPhotoThumbUrl?: string | null;
  planName?: string | null;
  planCode?: string | null;
  paymentStatus?: PaymentStatus;
  finalPrice?: number | null;
}

export interface MemorialAdmin {
  id: string;
  publicId: string;
  /** Canonical public page URL (same as QR target). */
  publicUrl?: string;
  fullName: string;
  mainPhoto?: PhotoRef | null;
  status: MemorialStatus;
  privacy: MemorialPrivacy;
  isDemo?: boolean;
  blocks: MemorialBlock[];
  callsign?: string | null;
  lifePeriod?: string | null;
  shortText?: string | null;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string | null;
  archivedAt?: string | null;
  planSnapshot?: PlanSnapshot | null;
  usedUpdates: number;
  qrPlateSize: QrPlateSize;
  qrPriceDeltaSnapshot: number;
  calculatedPrice?: number | null;
  finalPrice?: number | null;
  isFinalPriceOverridden: boolean;
  paymentStatus: PaymentStatus;
  paidAt?: string | null;
  usage?: PlanUsage | null;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface MemorialStatistics {
  publicId: string;
  totalViews: number;
  lastViewedAt?: string | null;
  viewsPerDay: { date: string; count: number }[];
}

export interface SiteSettings {
  phone: string;
  telegram: string;
  viber: string;
  additionalUpdatePrice: number;
  qrSize50PriceDelta: number;
  qrSize75PriceDelta: number;
  qrSize100PriceDelta: number;
  shortTextMaxChars: number;
  textBlockMaxChars: number;
  quoteMaxChars: number;
  timelineDescriptionMaxChars: number;
  memoryTextMaxChars: number;
  serviceDescriptionMaxChars: number;
  awardDescriptionMaxChars: number;
  photoCaptionMaxChars: number;
}

export interface PublicSiteSettings {
  phone: string;
  telegram: string;
  viber: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
}

export const STATUS_LABELS: Record<MemorialStatus, string> = {
  Draft: 'Чернетка',
  Published: 'Опубліковано',
  Archived: 'В архіві'
};

export const PRIVACY_LABELS: Record<MemorialPrivacy, string> = {
  Public: 'Публічна',
  Private: 'Приватна'
};

export const QR_PLATE_LABELS: Record<QrPlateSize, string> = {
  Size50: '50×50 мм',
  Size75: '75×75 мм',
  Size100: '100×100 мм'
};

export const PAYMENT_STATUS_LABELS: Record<PaymentStatus, string> = {
  Unpaid: 'Не оплачено',
  Paid: 'Оплачено'
};

export const BLOCK_TYPE_LABELS: Record<string, string> = {
  Text: 'Текст',
  Timeline: 'Життєвий шлях',
  Gallery: 'Галерея',
  Image: 'Велике фото',
  Quote: 'Цитата',
  Service: 'Служба',
  Awards: 'Відзнаки та нагороди',
  Memories: 'Спогади'
};

export function createEmptyBlockData(type: string): Record<string, unknown> {
  switch (type) {
    case 'Text':
      return { html: '<p></p>' };
    case 'Timeline':
      return { events: [] };
    case 'Gallery':
      return { items: [] };
    case 'Image':
      return { photoId: '', caption: '' };
    case 'Quote':
      return { text: '', author: '', authorDescription: '' };
    case 'Service':
      return { callsign: '', rank: '', unit: '', servicePeriod: '', description: '' };
    case 'Awards':
      return { items: [] };
    case 'Memories':
      return { items: [] };
    default:
      return {};
  }
}

export function isBlockEmpty(type: string, data: Record<string, unknown>): boolean {
  switch (type) {
    case 'Text': {
      const html = String(data['html'] ?? '')
        .replace(/<[^>]+>/g, '')
        .replace(/&nbsp;/g, ' ')
        .trim();
      return !html;
    }
    case 'Quote':
      return !String(data['text'] ?? '').trim();
    case 'Timeline':
      return !Array.isArray(data['events']) || data['events'].length === 0;
    case 'Gallery':
      return !Array.isArray(data['items']) || data['items'].length === 0;
    case 'Image':
      return !String(data['photoId'] ?? '').trim() && !((data['photo'] as PhotoRef | undefined)?.photoId);
    case 'Awards':
    case 'Memories':
      return !Array.isArray(data['items']) || data['items'].length === 0;
    case 'Service': {
      const keys = ['callsign', 'rank', 'unit', 'servicePeriod', 'description'];
      return keys.every((k) => !String(data[k] ?? '').trim());
    }
    default:
      return false;
  }
}
