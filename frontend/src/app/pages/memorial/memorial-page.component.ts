import { Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { ApiService } from '../../core/services/api.service';
import { PublicMemorial } from '../../core/models/memorial.models';
import { MemorialViewComponent } from '../../shared/components/memorial-view/memorial-view.component';
import { CanonicalService } from '../../core/services/canonical.service';
import { SsrResponseService } from '../../core/services/ssr-response.service';

@Component({
  selector: 'app-memorial-page',
  standalone: true,
  imports: [MemorialViewComponent],
  template: `
    @if (error) {
      <div class="flex min-h-screen flex-col items-center justify-center gap-3 bg-memorial-bg px-6 text-center">
        <h1 class="font-serif text-3xl">Сторінку не знайдено</h1>
        <p class="font-sans text-memorial-muted">Сторінку не знайдено</p>
        <a href="/" class="mt-4 font-serif text-memorial-muted underline">Nezabuti</a>
      </div>
    } @else if (memorial) {
      <app-memorial-view [memorial]="memorial" />
    }
  `
})
export class MemorialPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiService);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly canonical = inject(CanonicalService);
  private readonly ssr = inject(SsrResponseService);

  memorial: PublicMemorial | null = null;
  error = false;

  ngOnInit(): void {
    const resolved = this.route.snapshot.data['memorial'] as PublicMemorial | null;
    if (!resolved) {
      this.error = true;
      this.ssr.notFound();
      this.title.setTitle('Сторінку не знайдено — Nezabuti');
      this.meta.updateTag({ name: 'robots', content: 'noindex,nofollow' });
      this.canonical.clear();
      return;
    }

    this.memorial = resolved;
    this.applySeo(resolved);
    if (isPlatformBrowser(this.platformId)) {
      this.api.recordView(resolved.publicId, false).subscribe({ error: () => undefined });
    }
  }

  private applySeo(memorial: PublicMemorial): void {
    const seo = memorial.seo;
    this.title.setTitle(seo.title);
    this.meta.updateTag({ name: 'description', content: seo.description });
    this.meta.updateTag({ name: 'robots', content: seo.robots });
    this.canonical.set(seo.canonicalUrl);
    this.meta.updateTag({ property: 'og:title', content: seo.title });
    this.meta.updateTag({ property: 'og:description', content: seo.description });
    this.meta.updateTag({ property: 'og:url', content: seo.canonicalUrl });
    this.meta.updateTag({ property: 'og:type', content: 'website' });
    if (seo.ogImageUrl) {
      this.meta.updateTag({ property: 'og:image', content: seo.ogImageUrl });
    }
  }
}
