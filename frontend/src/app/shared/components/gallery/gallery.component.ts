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
  signal
} from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';

export interface GalleryItem {
  photoId: string;
  thumbUrl?: string;
  previewUrl: string;
  fullUrl?: string;
  caption?: string;
}

@Component({
  selector: 'app-gallery',
  standalone: true,
  template: `
    <div class="columns-1 gap-5 sm:columns-2 lg:columns-3">
      @for (item of items(); track item.photoId || $index; let i = $index) {
        <button
          type="button"
          class="mb-5 block w-full break-inside-avoid overflow-hidden text-left focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
          (click)="open(i)"
        >
          <img
            [src]="item.previewUrl || item.thumbUrl"
            [alt]="item.caption || 'Фотогалерея'"
            loading="lazy"
            decoding="async"
            class="w-full object-cover transition duration-500 motion-safe:hover:scale-[1.015]"
          />
          @if (item.caption) {
            <p class="mt-2.5 font-sans text-sm text-memorial-muted">{{ item.caption }}</p>
          }
        </button>
      }
    </div>

    @if (lightboxIndex() !== null) {
      <!-- Host stays in place; panel is moved to document.body so fixed = viewport -->
      <div #lightboxHost class="contents">
        <div
          #lightboxPanel
          class="gallery-lightbox group fixed inset-0 z-[100] flex h-[100dvh] max-h-[100dvh] w-full flex-col bg-black/55 backdrop-blur-[2px] md:bg-black/50"
          role="dialog"
          aria-modal="true"
          aria-label="Перегляд фото"
          (click)="close()"
          (touchstart)="onTouchStart($event)"
          (touchend)="onTouchEnd($event)"
        >
          <div
            class="pointer-events-none absolute inset-x-0 top-0 z-30 flex items-center justify-between px-3 py-3 sm:px-5 sm:py-4"
            (click)="$event.stopPropagation()"
          >
            <p
              class="pointer-events-none rounded-full bg-black/35 px-3 py-1 font-sans text-sm text-white tabular-nums shadow-sm sm:text-base"
            >
              {{ lightboxIndex()! + 1 }}&nbsp;/&nbsp;{{ items().length }}
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

          <div class="flex min-h-0 flex-1 items-center justify-center px-4 pb-8 pt-16 sm:px-16 sm:pb-10 sm:pt-20">
            <figure
              class="relative flex max-h-full max-w-full flex-col items-center"
              (click)="$event.stopPropagation()"
            >
              <img
                [src]="activeSrc()"
                [alt]="current().caption || 'Фото'"
                class="max-h-[calc(100dvh-7.5rem)] max-w-full object-contain select-none"
                draggable="false"
              />
              @if (current().caption) {
                <figcaption
                  class="mt-4 max-w-lg rounded-full bg-black/35 px-4 py-1.5 text-center font-sans text-sm text-white shadow-sm sm:text-base"
                >
                  {{ current().caption }}
                </figcaption>
              }
            </figure>
          </div>
        </div>
      </div>
    }
  `
})
export class GalleryComponent implements OnDestroy {
  readonly items = input<GalleryItem[]>([]);
  readonly lightboxIndex = signal<number | null>(null);

  @ViewChild('closeBtn') closeBtn?: ElementRef<HTMLButtonElement>;
  @ViewChild('lightboxPanel') lightboxPanel?: ElementRef<HTMLElement>;

  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly injector = inject(Injector);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private touchX = 0;
  private touchY = 0;
  private previousOverflow = '';
  private previousPaddingRight = '';
  private restoreFocusEl: HTMLElement | null = null;
  private readonly preloaded = new Set<string>();
  private panelOnBody: HTMLElement | null = null;

  constructor() {
    effect(() => {
      const index = this.lightboxIndex();
      if (!this.isBrowser) {
        return;
      }

      if (index === null) {
        this.detachPanelFromBody();
        return;
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
    this.detachPanelFromBody();
    this.unlockBodyScroll();
  }

  open(index: number): void {
    if (!this.items().length) {
      return;
    }
    if (this.isBrowser) {
      this.restoreFocusEl = this.document.activeElement as HTMLElement | null;
      this.lockBodyScroll();
    }
    this.lightboxIndex.set(index);
  }

  close(): void {
    if (this.lightboxIndex() === null) {
      return;
    }
    this.lightboxIndex.set(null);
    this.detachPanelFromBody();
    this.unlockBodyScroll();
    if (this.isBrowser && this.restoreFocusEl?.focus) {
      this.restoreFocusEl.focus();
    }
    this.restoreFocusEl = null;
  }

  current(): GalleryItem {
    return this.items()[this.lightboxIndex() ?? 0];
  }

  activeSrc(): string {
    const item = this.current();
    return item.fullUrl || item.previewUrl || item.thumbUrl || '';
  }

  prev(): void {
    const i = this.lightboxIndex();
    if (i === null) return;
    const len = this.items().length;
    if (len < 2) return;
    this.lightboxIndex.set((i - 1 + len) % len);
  }

  next(): void {
    const i = this.lightboxIndex();
    if (i === null) return;
    const len = this.items().length;
    if (len < 2) return;
    this.lightboxIndex.set((i + 1) % len);
  }

  @HostListener('document:keydown', ['$event'])
  onKey(event: KeyboardEvent): void {
    if (this.lightboxIndex() === null) return;
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
    if (this.lightboxIndex() === null || this.items().length < 2) return;
    const t = event.changedTouches[0];
    const dx = (t?.clientX ?? 0) - this.touchX;
    const dy = (t?.clientY ?? 0) - this.touchY;
    if (Math.abs(dx) < 56 || Math.abs(dx) < Math.abs(dy)) return;
    if (dx > 0) this.prev();
    else this.next();
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
    // Angular destroys the view node when @if becomes false; only remove if still on body
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
