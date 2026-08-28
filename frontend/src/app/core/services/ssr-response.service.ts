import { Meta } from '@angular/platform-browser';
import { Injectable, inject } from '@angular/core';

/**
 * SSR HTTP status via a meta marker read by Express after render.
 * Avoids InjectionToken identity issues across server/lazy bundles.
 */
@Injectable({ providedIn: 'root' })
export class SsrResponseService {
  private readonly meta = inject(Meta);

  setStatus(status: number): void {
    this.meta.updateTag({ name: 'ssr:status', content: String(status) });
  }

  notFound(): void {
    this.setStatus(404);
  }
}
