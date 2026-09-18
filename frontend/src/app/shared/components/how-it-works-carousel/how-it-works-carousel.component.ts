import { isPlatformBrowser } from '@angular/common';
import {
  Component,
  ElementRef,
  HostListener,
  PLATFORM_ID,
  computed,
  inject,
  input,
  signal,
  viewChild
} from '@angular/core';
import { PhotoRef, PublicHowItWorksSlide } from '../../../core/models/memorial.models';
import { LightboxComponent, LightboxItem } from '../lightbox/lightbox.component';

@Component({
  selector: 'app-how-it-works-carousel',
  standalone: true,
  imports: [LightboxComponent],
  template: `
    <div class="relative overflow-x-hidden" tabindex="0" role="region" aria-roledescription="карусель" aria-label="Як це працює">
      <div
        #track
        class="flex snap-x snap-mandatory overflow-x-auto overscroll-x-contain scroll-smooth pb-2 [scrollbar-width:none] touch-pan-x touch-pan-y [&::-webkit-scrollbar]:hidden max-md:gap-0 max-md:px-8 md:px-[100px]"
        (scroll)="onScroll()"
      >
        @for (slide of slides(); track $index; let i = $index) {
          <article
            class="shrink-0 snap-center overflow-hidden bg-memorial-bg transition duration-300 ease-out max-md:w-[calc(100%-4rem)] md:w-[calc(100%-200px)]"
            [class.opacity-100]="i === activeIndex()"
            [class.scale-100]="i === activeIndex()"
            [class.shadow-md]="i === activeIndex()"
            [class.opacity-50]="i !== activeIndex()"
            [class.scale-95]="i !== activeIndex()"
            [attr.aria-hidden]="i !== activeIndex() ? true : null"
            (click)="onCardClick(i)"
          >
            @if (desktopPhoto(slide)) {
              <button
                type="button"
                class="block w-full touch-pan-x touch-pan-y focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
                [attr.aria-label]="'Відкрити зображення: ' + (slide.altText || slide.title)"
                (click)="onImageClick(i, $event)"
              >
                <picture>
                  @if (hasDistinctMobile(slide)) {
                    <source
                      media="(max-width: 768px)"
                      [srcset]="srcset(mobilePhoto(slide)!)"
                      sizes="82vw"
                    />
                  }
                  <img
                    [src]="previewUrl(desktopPhoto(slide)!)"
                    [srcset]="srcset(desktopPhoto(slide)!)"
                    [sizes]="i === activeIndex() ? '(max-width: 768px) 82vw, 80vw' : '120px'"
                    [alt]="slide.altText || slide.title"
                    class="w-full object-contain max-md:aspect-[9/16] max-md:bg-memorial-bg md:aspect-video md:bg-memorial-surface"
                    [attr.width]="i === activeIndex() ? 1600 : 400"
                    [attr.height]="i === activeIndex() ? 900 : 225"
                    [attr.loading]="i === 0 ? 'eager' : 'lazy'"
                    [attr.fetchpriority]="i === 0 ? 'high' : 'low'"
                    [attr.decoding]="i === 0 ? 'sync' : 'async'"
                  />
                </picture>
              </button>
            }
          </article>
        }
      </div>

      @if (slides().length > 1) {
        <button
          type="button"
          class="absolute left-1 top-[42%] z-10 flex h-12 w-12 -translate-y-1/2 items-center justify-center border border-memorial-line bg-white/90 font-sans text-xl text-memorial-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent md:left-2 md:h-11 md:w-11"
          aria-label="Попередній слайд"
          (click)="go(-1)"
        >
          ‹
        </button>
        <button
          type="button"
          class="absolute right-1 top-[42%] z-10 flex h-12 w-12 -translate-y-1/2 items-center justify-center border border-memorial-line bg-white/90 font-sans text-xl text-memorial-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent md:right-2 md:h-11 md:w-11"
          aria-label="Наступний слайд"
          (click)="go(1)"
        >
          ›
        </button>
      }

      @if (active(); as slide) {
        <div class="mx-auto mt-6 max-w-3xl px-1 text-center md:mt-8">
          <p class="font-sans text-[0.7rem] uppercase tracking-[0.18em] text-memorial-muted">
            {{ activeIndex() + 1 }} / {{ slides().length }}
          </p>
          <h3 class="mt-2 font-serif text-2xl text-memorial-ink md:text-3xl">{{ slide.title }}</h3>
          @if (slide.description) {
            <p class="mt-3 font-sans text-sm leading-relaxed text-memorial-muted md:text-base">{{ slide.description }}</p>
          }
        </div>
      }

      @if (slides().length > 1) {
        <div class="mt-5 flex items-center justify-center gap-2" role="tablist" aria-label="Слайди">
          @for (slide of slides(); track $index; let i = $index) {
            <button
              type="button"
              class="h-2.5 w-2.5 border border-memorial-line focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
              [class.bg-memorial-ink]="i === activeIndex()"
              [class.bg-transparent]="i !== activeIndex()"
              [attr.aria-label]="'Слайд ' + (i + 1)"
              [attr.aria-current]="i === activeIndex() ? 'true' : null"
              (click)="goTo(i)"
            ></button>
          }
        </div>
      }
    </div>

    <app-lightbox
      [items]="lightboxItems()"
      [index]="lightboxIndex()"
      (indexChange)="onLightboxIndex($event)"
      (closed)="closeLightbox()"
    />
  `
})
export class HowItWorksCarouselComponent {
  readonly slides = input.required<PublicHowItWorksSlide[]>();
  private readonly track = viewChild<ElementRef<HTMLElement>>('track');
  private readonly platformId = inject(PLATFORM_ID);
  readonly activeIndex = signal(0);
  readonly lightboxIndex = signal<number | null>(null);
  readonly total = computed(() => this.slides().length);
  readonly active = computed(() => this.slides()[this.activeIndex()] ?? null);

  desktopPhoto(slide: PublicHowItWorksSlide): PhotoRef | null {
    return slide.desktopImage || slide.image || null;
  }

  mobilePhoto(slide: PublicHowItWorksSlide): PhotoRef | null {
    return slide.mobileImage || this.desktopPhoto(slide);
  }

  hasDistinctMobile(slide: PublicHowItWorksSlide): boolean {
    const mobile = slide.mobileImage;
    const desktop = this.desktopPhoto(slide);
    return !!mobile?.photoId && mobile.photoId !== desktop?.photoId;
  }

  previewUrl(photo: PhotoRef): string {
    return photo.previewUrl || photo.fullUrl || photo.thumbUrl || '';
  }

  srcset(photo: PhotoRef): string | null {
    const parts: string[] = [];
    if (photo.thumbUrl) parts.push(`${photo.thumbUrl} 320w`);
    if (photo.previewUrl) parts.push(`${photo.previewUrl} 800w`);
    if (photo.fullUrl) parts.push(`${photo.fullUrl} 2000w`);
    return parts.length ? parts.join(', ') : null;
  }

  lightboxItems(): LightboxItem[] {
    return this.slides().map((slide) => {
      const photo = this.lightboxPhoto(slide);
      return {
        photoId: photo?.photoId,
        thumbUrl: photo?.thumbUrl,
        previewUrl: photo?.previewUrl,
        fullUrl: photo?.fullUrl,
        caption: slide.title,
        alt: slide.altText || slide.title
      };
    });
  }

  onCardClick(index: number): void {
    if (index !== this.activeIndex()) {
      this.goTo(index);
    }
  }

  onImageClick(index: number, event: Event): void {
    event.stopPropagation();
    if (index !== this.activeIndex()) {
      this.goTo(index);
      return;
    }
    this.lightboxIndex.set(index);
  }

  onLightboxIndex(index: number): void {
    this.lightboxIndex.set(index);
    this.goTo(index);
  }

  closeLightbox(): void {
    this.lightboxIndex.set(null);
  }

  onScroll(): void {
    const el = this.track()?.nativeElement;
    if (!el || !this.slides().length) {
      return;
    }
    const children = Array.from(el.children) as HTMLElement[];
    if (!children.length) {
      return;
    }
    const center = el.scrollLeft + el.clientWidth / 2;
    let nearest = 0;
    let best = Number.POSITIVE_INFINITY;
    children.forEach((child, i) => {
      const childCenter = child.offsetLeft + child.offsetWidth / 2;
      const dist = Math.abs(childCenter - center);
      if (dist < best) {
        best = dist;
        nearest = i;
      }
    });
    this.activeIndex.set(nearest);
  }

  go(delta: number): void {
    const next = Math.min(this.slides().length - 1, Math.max(0, this.activeIndex() + delta));
    this.goTo(next);
  }

  goTo(index: number): void {
    const el = this.track()?.nativeElement;
    const child = el?.children.item(index) as HTMLElement | null;
    child?.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
    this.activeIndex.set(index);
  }

  @HostListener('keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (!isPlatformBrowser(this.platformId) || this.lightboxIndex() !== null) {
      return;
    }
    if (event.key === 'ArrowRight') {
      event.preventDefault();
      this.go(1);
    } else if (event.key === 'ArrowLeft') {
      event.preventDefault();
      this.go(-1);
    }
  }

  private lightboxPhoto(slide: PublicHowItWorksSlide): PhotoRef | null {
    if (!isPlatformBrowser(this.platformId)) {
      return this.desktopPhoto(slide);
    }
    return window.matchMedia('(max-width: 768px)').matches ? this.mobilePhoto(slide) : this.desktopPhoto(slide);
  }
}
