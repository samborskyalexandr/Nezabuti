import { Component, computed, input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { MemorialBlock, PhotoRef, isBlockEmpty } from '../../../core/models/memorial.models';
import { RevealDirective } from '../../directives/reveal.directive';
import { GalleryComponent } from '../gallery/gallery.component';

@Component({
  selector: 'app-block-renderer',
  standalone: true,
  imports: [RevealDirective, GalleryComponent],
  template: `
    <div class="mx-auto max-w-5xl px-4 sm:px-6 md:px-8">
      <div class="space-y-24 py-6 md:space-y-32 md:py-10">
        @for (block of visibleBlocks(); track block.id || block.order) {
          @switch (block.type) {
            @case ('Text') {
              <section class="mx-auto max-w-prose" appReveal="fade-up">
                <div class="prose-memorial" [innerHTML]="safeHtml(block)"></div>
              </section>
            }
            @case ('Quote') {
              <section class="mx-auto max-w-prose py-4" appReveal="fade-up">
                <blockquote class="text-center">
                  <p class="font-serif text-[1.65rem] font-medium leading-snug text-memorial-ink sm:text-3xl md:text-[2.15rem] md:leading-snug">
                    {{ asText(block, 'text') }}
                  </p>
                  @if (asText(block, 'author')) {
                    <footer class="mt-8 font-sans text-sm text-memorial-muted">
                      <span>{{ asText(block, 'author') }}</span>
                      @if (asText(block, 'authorDescription')) {
                        <span class="block mt-1 text-memorial-muted/80">{{ asText(block, 'authorDescription') }}</span>
                      }
                    </footer>
                  }
                </blockquote>
              </section>
            }
            @case ('Service') {
              <section class="mx-auto max-w-prose" appReveal="fade-up">
                <h2 class="font-serif text-3xl font-semibold tracking-tight md:text-4xl">На захисті України</h2>
                <dl class="mt-10 space-y-6 font-sans">
                  @if (asText(block, 'callsign')) {
                    <div>
                      <dt class="text-xs uppercase tracking-[0.18em] text-memorial-muted">Позивний</dt>
                      <dd class="mt-1.5 text-lg text-memorial-ink">{{ asText(block, 'callsign') }}</dd>
                    </div>
                  }
                  @if (asText(block, 'rank')) {
                    <div>
                      <dt class="text-xs uppercase tracking-[0.18em] text-memorial-muted">Звання</dt>
                      <dd class="mt-1.5 text-lg text-memorial-ink">{{ asText(block, 'rank') }}</dd>
                    </div>
                  }
                  @if (asText(block, 'unit')) {
                    <div>
                      <dt class="text-xs uppercase tracking-[0.18em] text-memorial-muted">Підрозділ</dt>
                      <dd class="mt-1.5 text-lg text-memorial-ink">{{ asText(block, 'unit') }}</dd>
                    </div>
                  }
                  @if (asText(block, 'servicePeriod')) {
                    <div>
                      <dt class="text-xs uppercase tracking-[0.18em] text-memorial-muted">Період служби</dt>
                      <dd class="mt-1.5 text-lg text-memorial-ink">{{ asText(block, 'servicePeriod') }}</dd>
                    </div>
                  }
                  @if (asText(block, 'description')) {
                    <p class="pt-2 text-[1.05rem] leading-relaxed text-memorial-ink/90">{{ asText(block, 'description') }}</p>
                  }
                </dl>
              </section>
            }
            @case ('Timeline') {
              <section class="mx-auto max-w-prose" appReveal="timeline">
                <h2 class="font-serif text-3xl font-semibold tracking-tight md:text-4xl">Життєвий шлях</h2>
                <ol class="relative mt-12 space-y-12 border-l border-memorial-line pl-8 md:space-y-14">
                  @for (event of asArray(block, 'events'); track $index) {
                    <li class="relative" appReveal="fade-up">
                      <span
                        class="absolute -left-[2.15rem] top-1.5 h-2.5 w-2.5 rounded-full bg-memorial-accent ring-4 ring-memorial-bg"
                        aria-hidden="true"
                      ></span>
                      @if (event['dateOrPeriod']) {
                        <p class="font-sans text-sm font-medium tracking-wide text-memorial-muted">
                          {{ event['dateOrPeriod'] }}
                        </p>
                      }
                      @if (event['title']) {
                        <h3 class="mt-1.5 font-serif text-2xl font-semibold leading-snug">{{ event['title'] }}</h3>
                      }
                      @if (event['description']) {
                        <p class="mt-3 font-sans text-[1.05rem] leading-relaxed text-memorial-ink/90">
                          {{ event['description'] }}
                        </p>
                      }
                      @if (photoPreview(event['photo'])) {
                        <img
                          [src]="photoPreview(event['photo'])!"
                          [alt]="asAlt(event['title'], 'Подія')"
                          class="mt-5 w-full max-w-md object-cover"
                          loading="lazy"
                          decoding="async"
                        />
                      }
                    </li>
                  }
                </ol>
              </section>
            }
            @case ('Gallery') {
              <section>
                <div class="mx-auto mb-10 max-w-prose" appReveal="fade-up">
                  <h2 class="font-serif text-3xl font-semibold tracking-tight md:text-4xl">Галерея</h2>
                </div>
                <app-gallery [items]="galleryItems(block)" />
              </section>
            }
            @case ('Image') {
              <figure class="mx-auto max-w-4xl" appReveal="scale">
                <img
                  [src]="imagePreview(block) || imageFull(block)"
                  [srcset]="imageSrcset(block)"
                  sizes="(max-width: 768px) 100vw, 896px"
                  [alt]="asText(block, 'caption') || 'Фото'"
                  class="w-full object-cover"
                  loading="lazy"
                  decoding="async"
                />
                @if (asText(block, 'caption')) {
                  <figcaption class="mx-auto mt-4 max-w-prose text-center font-sans text-sm text-memorial-muted">
                    {{ asText(block, 'caption') }}
                  </figcaption>
                }
              </figure>
            }
            @case ('Awards') {
              <section class="mx-auto max-w-prose" appReveal="fade-up">
                <h2 class="font-serif text-3xl font-semibold tracking-tight md:text-4xl">Нагороди</h2>
                <ul class="mt-12 space-y-12">
                  @for (item of asArray(block, 'items'); track $index) {
                    <li class="flex flex-col gap-5 sm:flex-row sm:gap-8">
                      @if (photoPreview(item['photo'])) {
                        <img
                          [src]="photoPreview(item['photo'])!"
                          [alt]="asAlt(item['name'], 'Нагорода')"
                          class="h-28 w-28 shrink-0 object-cover sm:h-32 sm:w-32"
                          loading="lazy"
                          decoding="async"
                        />
                      }
                      <div class="min-w-0">
                        <h3 class="font-serif text-2xl font-semibold leading-snug">{{ item['name'] }}</h3>
                        @if (item['yearOrDate']) {
                          <p class="mt-1.5 font-sans text-sm text-memorial-muted">{{ item['yearOrDate'] }}</p>
                        }
                        @if (item['description']) {
                          <p class="mt-3 font-sans leading-relaxed text-memorial-ink/90">{{ item['description'] }}</p>
                        }
                      </div>
                    </li>
                  }
                </ul>
              </section>
            }
            @case ('Memories') {
              <section class="mx-auto max-w-prose" appReveal="fade-up">
                <h2 class="font-serif text-3xl font-semibold tracking-tight md:text-4xl">Спогади</h2>
                <ul class="mt-12 space-y-14">
                  @for (item of asArray(block, 'items'); track $index) {
                    <li class="border-t border-memorial-line pt-10">
                      @if (photoThumb(item['photo'])) {
                        <img
                          [src]="photoThumb(item['photo'])!"
                          [alt]="asAlt(item['author'], 'Спогад')"
                          class="mb-6 h-20 w-20 object-cover"
                          loading="lazy"
                          decoding="async"
                        />
                      }
                      <p class="font-serif text-xl leading-relaxed text-memorial-ink sm:text-2xl">
                        {{ item['text'] }}
                      </p>
                      <p class="mt-5 font-sans text-sm text-memorial-muted">
                        {{ item['author'] }}
                        @if (item['relationOrDescription']) {
                          <span> · {{ item['relationOrDescription'] }}</span>
                        }
                      </p>
                    </li>
                  }
                </ul>
              </section>
            }
            @default {
              <!-- Unknown block types are skipped without breaking the page -->
            }
          }
        }
      </div>
    </div>
  `
})
export class BlockRendererComponent {
  readonly blocks = input<MemorialBlock[]>([]);
  readonly visibleBlocks = computed(() =>
    (this.blocks() ?? []).filter((b) => !isBlockEmpty(b.type, b.data ?? {}))
  );

  constructor(private readonly sanitizer: DomSanitizer) {}

  asAlt(value: unknown, fallback: string): string {
    const text = value == null ? '' : String(value).trim();
    return text || fallback;
  }

  safeHtml(block: MemorialBlock): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(String(block.data['html'] ?? ''));
  }

  asText(block: MemorialBlock, key: string): string {
    const value = block.data[key];
    return value == null ? '' : String(value);
  }

  asArray(block: MemorialBlock, key: string): Record<string, unknown>[] {
    const value = block.data[key];
    return Array.isArray(value) ? (value as Record<string, unknown>[]) : [];
  }

  galleryItems(block: MemorialBlock) {
    return this.asArray(block, 'items').map((item) => ({
      photoId: String(item['photoId'] ?? ''),
      thumbUrl: item['thumbUrl'] ? String(item['thumbUrl']) : undefined,
      previewUrl: String(item['previewUrl'] ?? item['thumbUrl'] ?? ''),
      fullUrl: item['fullUrl'] ? String(item['fullUrl']) : undefined,
      caption: item['caption'] ? String(item['caption']) : undefined
    }));
  }

  photoPreview(photo: unknown): string | null {
    if (!photo || typeof photo !== 'object') return null;
    const p = photo as PhotoRef;
    return p.previewUrl || p.thumbUrl || null;
  }

  photoThumb(photo: unknown): string | null {
    if (!photo || typeof photo !== 'object') return null;
    const p = photo as PhotoRef;
    return p.thumbUrl || p.previewUrl || null;
  }

  imagePreview(block: MemorialBlock): string {
    const photo = block.data['photo'] as PhotoRef | undefined;
    return String(block.data['previewUrl'] ?? photo?.previewUrl ?? '');
  }

  imageFull(block: MemorialBlock): string {
    const photo = block.data['photo'] as PhotoRef | undefined;
    return String(block.data['fullUrl'] ?? photo?.fullUrl ?? '');
  }

  imageSrcset(block: MemorialBlock): string | null {
    const photo = (block.data['photo'] as PhotoRef | undefined) ?? {
      photoId: String(block.data['photoId'] ?? ''),
      thumbUrl: String(block.data['thumbUrl'] ?? ''),
      previewUrl: String(block.data['previewUrl'] ?? ''),
      fullUrl: String(block.data['fullUrl'] ?? '')
    };
    const parts: string[] = [];
    if (photo.thumbUrl) parts.push(`${photo.thumbUrl} 320w`);
    if (photo.previewUrl) parts.push(`${photo.previewUrl} 800w`);
    if (photo.fullUrl) parts.push(`${photo.fullUrl} 2000w`);
    return parts.length ? parts.join(', ') : null;
  }
}
