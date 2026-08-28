import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';

/**
 * Manages a real <link rel="canonical"> element for SSR and client navigation.
 */
@Injectable({ providedIn: 'root' })
export class CanonicalService {
  private readonly document = inject(DOCUMENT);

  set(url: string): void {
    const head = this.document.head;
    if (!head) {
      return;
    }

    let link = head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!link) {
      link = this.document.createElement('link');
      link.setAttribute('rel', 'canonical');
      head.appendChild(link);
    }

    link.setAttribute('href', url);
  }

  clear(): void {
    const link = this.document.head?.querySelector('link[rel="canonical"]');
    link?.remove();
  }
}
