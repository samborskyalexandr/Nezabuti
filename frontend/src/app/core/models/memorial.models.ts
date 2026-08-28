export type MemorialStatus = 'Draft' | 'Published' | 'Archived';
export type MemorialPrivacy = 'Public' | 'Private';

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
  blocks: MemorialBlock[];
  callsign?: string | null;
  lifePeriod?: string | null;
  shortText?: string | null;
  publishedAt?: string | null;
  seo: SeoMeta;
}

export interface MemorialListItem {
  id: string;
  publicId: string;
  fullName: string;
  status: MemorialStatus;
  privacy: MemorialPrivacy;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string | null;
  archivedAt?: string | null;
  mainPhotoPreviewUrl?: string | null;
  mainPhotoThumbUrl?: string | null;
}

export interface MemorialAdmin {
  id: string;
  publicId: string;
  fullName: string;
  mainPhoto?: PhotoRef | null;
  status: MemorialStatus;
  privacy: MemorialPrivacy;
  blocks: MemorialBlock[];
  callsign?: string | null;
  lifePeriod?: string | null;
  shortText?: string | null;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string | null;
  archivedAt?: string | null;
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

export const BLOCK_TYPE_LABELS: Record<string, string> = {
  Text: 'Текст',
  Timeline: 'Життєвий шлях',
  Gallery: 'Галерея',
  Image: 'Велике фото',
  Quote: 'Цитата',
  Service: 'Служба',
  Awards: 'Нагороди',
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
