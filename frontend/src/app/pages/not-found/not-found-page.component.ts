import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { SsrResponseService } from '../../core/services/ssr-response.service';
import { CanonicalService } from '../../core/services/canonical.service';

@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-4 bg-memorial-bg px-6 text-center">
      <p class="font-serif text-5xl text-memorial-ink">404</p>
      <h1 class="font-serif text-2xl text-memorial-ink">Сторінку не знайдено</h1>
      <a
        routerLink="/"
        class="mt-4 font-serif text-memorial-muted underline transition hover:text-memorial-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
      >
        Nezabuti
      </a>
    </div>
  `
})
export class NotFoundPageComponent implements OnInit {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly ssr = inject(SsrResponseService);
  private readonly canonical = inject(CanonicalService);

  ngOnInit(): void {
    this.ssr.notFound();
    this.title.setTitle('Сторінку не знайдено — Nezabuti');
    this.meta.updateTag({ name: 'robots', content: 'noindex,nofollow' });
    this.canonical.clear();
  }
}
