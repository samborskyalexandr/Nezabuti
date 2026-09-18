import {
  Component,
  ElementRef,
  HostListener,
  Injector,
  OnDestroy,
  PLATFORM_ID,
  ViewChild,
  afterNextRender,
  effect,
  inject,
  input,
  output,
  signal
} from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';

export interface LightboxItem {
  photoId?: string;
  thumbUrl?: string;
  previewUrl?: string;
  fullUrl?: string;
  caption?: string;
  alt?: string;
}

@Component({
  selector: 'app-lightbox',
  standalone: true,
  template: `
    @if (index() !== null) {
      <div #lightboxHost class="contents">
        <div
          #lightboxPanel
          class="gallery-lightbox group fixed inset-0 z-[100] flex h-[100dvh] max-h-[100dvh] w-full flex-col bg-black/55 backdrop-blur-[2px] md:bg-black/50"
          role="dialog"
          aria-modal="true"
          aria-label="Перегляд зображення"
          (click)="close()"
          (touchstart)="onTouchStart($event)"
          (touchend)="onTouchEnd($event)"
        >
          <div
            class="pointer-events-none absolute inset-x-0 top-0 z-30 flex items-center justify-between px-3 py-3 sm:px-5 sm:py-4"
            (click)="$event.stopPropagation()"
          >
            <p class="pointer-events-none rounded-full bg-black/35 px-3 py-1 font-sans text-sm text-white tabular-nums shadow-sm sm:text-base">
              {{ (index() ?? 0) + 1 }}&nbsp;/&nbsp;{{ items().length }}
            </p>
            <button
              #closeBtn
              type="button"
              class="pointer-events-auto flex h-11 w-11 items-center justify-center rounded-full bg-black/40 text-2xl leading-none text-white shadow-sm transition hover:bg-black/55 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white sm:h-12 sm:w-12"
              aria-label="Закрити"
              (click)="close()"
            >
              <span aria-hidden="true">×</span>
            </button>
          </div>

          @if (items().length > 1) {
            <button
              type="button"
              class="absolute left-2 top-1/2 z-30 flex h-12 w-12 -translate-y-1/2 items-center justify-center rounded-full bg-black/40 text-2xl text-white shadow-sm transition hover:bg-black/55 focus-visible:opacity-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white sm:left-4 sm:h-14 sm:w-14 md:opacity-0 md:group-hover:opacity-100 md:group-focus-within:opacity-100"
              aria-label="Попереднє фото"
              (click)="prev(); $event.stopPropagation()"
            >
              <span aria-hidden="true">‹</span>
            </button>
            <button
              type="button"
              class="absolute right-2 top-1/2 z-30 flex h-12 w-12 -translate-y-1/2 items-center justify-center rounded-full bg-black/40 text-2xl text-white shadow-sm transition hover:bg-black/55 focus-visible:opacity-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white sm:right-4 sm:h-14 sm:w-14 md:opacity-0 md:group-hover:opacity-100 md:group-focus-within:opacity-100"
              aria-label="Наступне фото"
              (click)="next(); $event.stopPropagation()"
            >
              <span aria-hidden="true">›</span>
            </button>
          }

          <div class="grid min-h-0 flex-1 px-4 py-8 md:px-16 md:py-20">
            <figure
              class="flex h-full min-h-0 w-full flex-col items-center justify-center"
              (click)="$event.stopPropagation()"
            >
              <div class="flex min-h-0 flex-1 items-center justify-center">
                <img
                  [src]="activeSrc()"
                  [alt]="current()?.alt || current()?.caption || 'Зображення'"
                  class="max-h-full max-w-full object-contain select-none"
                  draggable="false"
                />
              </div>
              @if (current()?.caption) {
                <figcaption class="mt-2 shrink-0 max-w-lg rounded-full bg-black/35 px-4 py-1.5 text-center font-sans text-sm text-white shadow-sm md:mt-4 sm:text-base">
                  {{ current()?.caption }}
                </figcaption>
              }
            </figure>
          </div>
        </div>
      </div>
    }
  `
})
export class LightboxComponent implements OnDestroy {
  readonly items = input<LightboxItem[]>([]);
  readonly index = input<number | null>(null);
  readonly indexChange = output<number>();
  readonly closed = output<void>();

  @ViewChild('closeBtn') closeBtn?: ElementRef<HTMLButtonElement>;
  @ViewChild('lightboxPanel') lightboxPanel?: ElementRef<HTMLElement>;

  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly injector = inject(Injector);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  private readonly open = signal(false);

  private touchX = 0;
  private touchY = 0;
  private previousOverflow = '';
  private previousPaddingRight = '';
  private restoreFocusEl: HTMLElement | null = null;
  private readonly preloaded = new Set<string>();
  private panelOnBody: HTMLElement | null = null;

  constructor() {
    effect(() => {
      const index = this.index();
      if (!this.isBrowser) {
        return;
      }

      if (index === null) {
        this.tearDown();
        return;
      }

      if (!this.open()) {
        this.restoreFocusEl = this.document.activeElement as HTMLElement | null;
        this.lockBodyScroll();
        this.open.set(true);
      }

      this.preloadAround(index);
      afterNextRender(
        () => {
          this.attachPanelToBody();
          this.closeBtn?.nativeElement.focus();
        },
        { injector: this.injector }
      );
    });
  }

  ngOnDestroy(): void {
    this.tearDown();
  }

  current(): LightboxItem | undefined {
    return this.items()[this.index() ?? 0];
  }

  activeSrc(): string {
    const item = this.current();
    return item?.fullUrl || item?.previewUrl || item?.thumbUrl || '';
  }

  close(): void {
    if (this.index() === null) {
      return;
    }
    this.tearDown();
    this.closed.emit();
    if (this.isBrowser && this.restoreFocusEl?.focus) {
      this.restoreFocusEl.focus();
    }
    this.restoreFocusEl = null;
  }

  prev(): void {
    const i = this.index();
    const len = this.items().length;
    if (i === null || len < 2) return;
    this.indexChange.emit((i - 1 + len) % len);
  }

  next(): void {
    const i = this.index();
    const len = this.items().length;
    if (i === null || len < 2) return;
    this.indexChange.emit((i + 1) % len);
  }

  @HostListener('document:keydown', ['$event'])
  onKey(event: KeyboardEvent): void {
    if (this.index() === null) return;
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
    } else if (event.key === 'ArrowLeft') {
      event.preventDefault();
      this.prev();
    } else if (event.key === 'ArrowRight') {
      event.preventDefault();
      this.next();
    }
  }

  onTouchStart(event: TouchEvent): void {
    const t = event.changedTouches[0];
    this.touchX = t?.clientX ?? 0;
    this.touchY = t?.clientY ?? 0;
  }

  onTouchEnd(event: TouchEvent): void {
    if (this.index() === null || this.items().length < 2) return;
    const t = event.changedTouches[0];
    const dx = (t?.clientX ?? 0) - this.touchX;
    const dy = (t?.clientY ?? 0) - this.touchY;
    if (Math.abs(dx) < 56 || Math.abs(dx) < Math.abs(dy)) return;
    if (dx > 0) this.prev();
    else this.next();
  }

  private tearDown(): void {
    this.detachPanelFromBody();
    this.unlockBodyScroll();
    this.open.set(false);
  }

  private attachPanelToBody(): void {
    const panel = this.lightboxPanel?.nativeElement;
    if (!panel || !this.isBrowser) return;
    if (panel.parentElement === this.document.body) {
      this.panelOnBody = panel;
      return;
    }
    this.document.body.appendChild(panel);
    this.panelOnBody = panel;
  }

  private detachPanelFromBody(): void {
    if (!this.isBrowser || !this.panelOnBody) {
      this.panelOnBody = null;
      return;
    }
    if (this.panelOnBody.parentElement === this.document.body) {
      this.document.body.removeChild(this.panelOnBody);
    }
    this.panelOnBody = null;
  }

  private preloadAround(index: number): void {
    const list = this.items();
    const len = list.length;
    if (!len) return;
    const targets = len === 1 ? [index] : [index, (index - 1 + len) % len, (index + 1) % len];
    for (const i of targets) {
      const url = list[i]?.fullUrl;
      if (!url || this.preloaded.has(url)) continue;
      this.preloaded.add(url);
      const img = new Image();
      img.decoding = 'async';
      img.src = url;
    }
  }

  private lockBodyScroll(): void {
    if (!this.isBrowser) return;
    const body = this.document.body;
    const scrollbar = window.innerWidth - this.document.documentElement.clientWidth;
    this.previousOverflow = body.style.overflow;
    this.previousPaddingRight = body.style.paddingRight;
    body.style.overflow = 'hidden';
    if (scrollbar > 0) {
      body.style.paddingRight = `${scrollbar}px`;
    }
  }

  private unlockBodyScroll(): void {
    if (!this.isBrowser) return;
    const body = this.document.body;
    body.style.overflow = this.previousOverflow;
    body.style.paddingRight = this.previousPaddingRight;
  }
}
