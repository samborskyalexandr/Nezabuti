import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
  Directive,
  ElementRef,
  OnDestroy,
  OnInit,
  PLATFORM_ID,
  inject,
  input
} from '@angular/core';

/**
 * One-shot reveal when element enters the viewport. Respects prefers-reduced-motion.
 */
@Directive({
  selector: '[appReveal]',
  standalone: true
})
export class RevealDirective implements OnInit, OnDestroy {
  readonly appReveal = input<'fade-in' | 'fade-up' | 'scale' | 'timeline'>('fade-up');
  private readonly el = inject(ElementRef<HTMLElement>);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly document = inject(DOCUMENT);
  private observer: IntersectionObserver | null = null;

  ngOnInit(): void {
    const host = this.el.nativeElement;
    const reduced =
      isPlatformBrowser(this.platformId) &&
      this.document.defaultView?.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (reduced || !isPlatformBrowser(this.platformId) || typeof IntersectionObserver === 'undefined') {
      host.classList.add('is-revealed');
      return;
    }

    host.classList.add('reveal', `reveal-${this.appReveal()}`);
    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            host.classList.add('is-revealed');
            this.observer?.disconnect();
            this.observer = null;
            break;
          }
        }
      },
      { threshold: 0.12, rootMargin: '0px 0px -8% 0px' }
    );
    this.observer.observe(host);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
