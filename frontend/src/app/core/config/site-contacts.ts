/**
 * Helpers for rendering public contact links from SiteSettings values.
 * Contact values themselves come from GET /api/public/settings (MongoDB).
 */

export function phoneTelHref(phone: string): string | null {
  const trimmed = phone.trim();
  if (!trimmed) return null;
  const normalized = trimmed.replace(/[^\d+]/g, '');
  if (!normalized) return null;
  return `tel:${normalized}`;
}

/** Accepts full HTTPS URL, t.me/…, or @username / username. */
export function telegramHref(telegram: string): string | null {
  const trimmed = telegram.trim();
  if (!trimmed) return null;
  if (/^https?:\/\//i.test(trimmed)) return trimmed;
  const withoutAt = trimmed.replace(/^@/, '');
  if (/^(t\.me|telegram\.me)\//i.test(withoutAt)) {
    return `https://${withoutAt}`;
  }
  return `https://t.me/${withoutAt}`;
}

export function telegramLabel(telegram: string): string {
  const trimmed = telegram.trim();
  if (!trimmed) return '';
  if (/^https?:\/\//i.test(trimmed)) {
    try {
      const path = new URL(trimmed).pathname.replace(/^\//, '');
      return path ? `@${path.split('/')[0]}` : trimmed;
    } catch {
      return trimmed;
    }
  }
  return trimmed.startsWith('@') ? trimmed : `@${trimmed.replace(/^(t\.me|telegram\.me)\//i, '')}`;
}

export function viberChatLink(viber: string): string | null {
  const trimmed = viber.trim();
  if (!trimmed) return null;
  const normalized = trimmed.replace(/[^\d+]/g, '');
  if (!normalized) return null;
  return `viber://chat?number=${encodeURIComponent(normalized)}`;
}
