import { Component, computed, input } from '@angular/core';
import { RevealDirective } from '../../directives/reveal.directive';

@Component({
  selector: 'app-memorial-hero',
  standalone: true,
  imports: [RevealDirective],
  template: `
    <section class="relative" appReveal="fade-in">
      <!-- Subtle tonal wash from photo (desktop), never overpowering -->
      @if (previewUrl() || thumbUrl()) {
        <div
          class="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[55vh] overflow-hidden opacity-[0.18]"
          aria-hidden="true"
        >
          <img
            [src]="previewUrl() || thumbUrl()!"
            alt=""
            class="h-full w-full scale-110 object-cover blur-2xl"
          />
          <div class="absolute inset-0 bg-gradient-to-b from-memorial-bg/40 via-memorial-bg/70 to-memorial-bg"></div>
        </div>
      }

      <div class="mx-auto max-w-5xl px-4 pb-10 pt-6 sm:px-6 md:px-8 md:pb-16 md:pt-10">
        @if (hasPhoto()) {
          <div class="overflow-hidden">
            <img
              [src]="heroSrc()"
              [srcset]="srcset()"
              sizes="(max-width: 640px) 100vw, (max-width: 1024px) 90vw, 960px"
              [alt]="fullName()"
              width="960"
              height="1200"
              class="aspect-[4/5] w-full object-cover sm:aspect-[5/6] md:aspect-[4/5] lg:max-h-[78vh]"
              [attr.fetchpriority]="'high'"
              decoding="async"
            />
          </div>
        }

        <div class="mx-auto mt-8 max-w-memorial text-center md:mt-12">
          @if (trimmed(callsign())) {
            <p class="mb-3 font-sans text-[0.7rem] font-medium uppercase tracking-[0.28em] text-memorial-muted sm:text-xs">
              {{ trimmed(callsign()) }}
            </p>
          }
          <h1 class="font-serif text-[2.35rem] font-semibold leading-[1.12] text-memorial-ink sm:text-5xl md:text-6xl lg:text-[4rem]">
            {{ fullName() }}
          </h1>
          @if (trimmed(lifePeriod())) {
            <p class="mt-4 font-sans text-base text-memorial-muted sm:text-lg">{{ trimmed(lifePeriod()) }}</p>
          }
          @if (trimmed(shortText())) {
            <p class="mx-auto mt-6 max-w-xl font-sans text-[1.05rem] leading-relaxed text-memorial-ink/90 sm:text-lg">
              {{ trimmed(shortText()) }}
            </p>
          }
        </div>
      </div>
    </section>
  `
})
export class MemorialHeroComponent {
  readonly fullName = input.required<string>();
  readonly photoUrl = input<string | null | undefined>(null);
  readonly thumbUrl = input<string | null | undefined>(null);
  readonly previewUrl = input<string | null | undefined>(null);
  readonly fullUrl = input<string | null | undefined>(null);
  readonly callsign = input<string | null | undefined>(null);
  readonly lifePeriod = input<string | null | undefined>(null);
  readonly shortText = input<string | null | undefined>(null);

  readonly hasPhoto = computed(
    () => !!(this.fullUrl() || this.previewUrl() || this.photoUrl() || this.thumbUrl())
  );

  heroSrc(): string {
    return this.previewUrl() || this.fullUrl() || this.photoUrl() || this.thumbUrl() || '';
  }

  srcset(): string | null {
    const parts: string[] = [];
    if (this.thumbUrl()) parts.push(`${this.thumbUrl()} 320w`);
    if (this.previewUrl()) parts.push(`${this.previewUrl()} 800w`);
    if (this.fullUrl()) parts.push(`${this.fullUrl()} 2000w`);
    return parts.length ? parts.join(', ') : null;
  }

  trimmed(value: string | null | undefined): string {
    return (value ?? '').trim();
  }
}
