import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PublicMemorial } from '../../../core/models/memorial.models';
import { MemorialHeroComponent } from '../memorial-hero/memorial-hero.component';
import { BlockRendererComponent } from '../block-renderer/block-renderer.component';
import { RevealDirective } from '../../directives/reveal.directive';

/**
 * Shared public memorial rendering used by /m/:publicId and admin preview.
 */
@Component({
  selector: 'app-memorial-view',
  standalone: true,
  imports: [RouterLink, MemorialHeroComponent, BlockRendererComponent, RevealDirective],
  template: `
    <article class="min-h-screen bg-memorial-bg">
      <header class="px-4 pt-5 sm:px-6 md:px-8">
        <div class="mx-auto max-w-5xl">
          <a
            routerLink="/"
            class="inline-block font-serif text-lg tracking-wide text-memorial-muted transition hover:text-memorial-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
          >
            Nezabuti
          </a>
        </div>
      </header>

      <app-memorial-hero
        [fullName]="memorial().fullName"
        [photoUrl]="memorial().mainPhoto?.fullUrl || memorial().mainPhoto?.previewUrl"
        [thumbUrl]="memorial().mainPhoto?.thumbUrl"
        [previewUrl]="memorial().mainPhoto?.previewUrl"
        [fullUrl]="memorial().mainPhoto?.fullUrl"
        [callsign]="memorial().callsign"
        [lifePeriod]="memorial().lifePeriod"
        [shortText]="memorial().shortText"
      />

      <app-block-renderer [blocks]="memorial().blocks" />

      <section class="px-4 py-24 text-center md:py-32" appReveal="fade-up">
        <div class="mx-auto max-w-prose">
          <p class="font-serif text-2xl text-memorial-ink/80 md:text-3xl">Світла пам’ять</p>
          <p class="mt-5 font-serif text-3xl font-semibold text-memorial-ink md:text-4xl">{{ memorial().fullName }}</p>
          @if (memorial().lifePeriod) {
            <p class="mt-3 font-sans text-memorial-muted">{{ memorial().lifePeriod }}</p>
          }
          <a
            routerLink="/"
            class="mt-14 inline-block font-serif text-xl tracking-wide text-memorial-muted transition hover:text-memorial-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
          >
            Nezabuti
          </a>
        </div>
      </section>
    </article>
  `
})
export class MemorialViewComponent {
  readonly memorial = input.required<PublicMemorial>();
}
