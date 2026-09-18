/** Public demo CTA may be an http(s) URL or a same-site path like /m/ABC. */
export function isAllowedDemoUrl(url: string | null | undefined): boolean {
  if (!url || !url.trim()) {
    return false;
  }

  const trimmed = url.trim();
  if (trimmed.startsWith('/') && !trimmed.startsWith('//')) {
    return trimmed.length > 1 && !trimmed.includes('\\');
  }

  try {
    const parsed = new URL(trimmed);
    return parsed.protocol === 'http:' || parsed.protocol === 'https:';
  } catch {
    return false;
  }
}
