import { HttpInterceptorFn } from '@angular/common/http';
import { PLATFORM_ID, inject } from '@angular/core';
import { isPlatformServer } from '@angular/common';

/**
 * On SSR, relative /api and /uploads must hit the Docker service `backend`.
 */
export const ssrApiInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);
  if (!isPlatformServer(platformId)) {
    return next(req);
  }

  if (req.url.startsWith('/api') || req.url.startsWith('/uploads')) {
    const base = (globalThis as { process?: { env?: Record<string, string> } }).process?.env?.['SSR_API_ORIGIN']
      || 'http://backend:8080';
    return next(req.clone({ url: `${base}${req.url}` }));
  }

  return next(req);
};
