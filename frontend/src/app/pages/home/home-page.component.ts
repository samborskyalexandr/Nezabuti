import { DOCUMENT } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { RevealDirective } from '../../shared/directives/reveal.directive';
import { CanonicalService } from '../../core/services/canonical.service';
import { ApiService } from '../../core/services/api.service';
import { SiteSettings } from '../../core/models/memorial.models';
import {
  phoneTelHref,
  telegramHref,
  telegramLabel,
  viberChatLink
} from '../../core/config/site-contacts';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [RouterLink, RevealDirective],
  template: `
    <div class="min-h-screen bg-memorial-bg">
      <header class="absolute inset-x-0 top-0 z-10 px-6 py-6 md:px-10">
        <div class="mx-auto flex max-w-6xl items-center justify-between gap-6">
          <a routerLink="/" class="font-serif text-2xl tracking-wide text-memorial-ink">Nezabuti</a>
          <nav class="font-sans text-sm text-memorial-muted">
            <a
              href="#contacts"
              class="transition hover:text-memorial-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
            >
              Контакти
            </a>
          </nav>
        </div>
      </header>

      <section
        class="relative bg-gradient-to-br from-[#EFE8DC] via-[#F3EEE6] to-[#E4EBE3] px-6 pb-14 pt-44 md:px-10 md:pb-20 md:pt-60"
        appReveal="fade-in"
      >
        <div class="mx-auto max-w-6xl">
          <p class="font-serif text-5xl font-semibold leading-tight md:text-7xl">Nezabuti</p>
          <p class="mt-5 max-w-xl font-sans text-lg text-memorial-muted md:text-xl">
            Цифрові меморіальні сторінки про полеглих — місце пам’яті, яке завжди з собою.
          </p>
          <div class="mt-8">
            <a
              href="#about"
              class="inline-block border border-memorial-ink px-6 py-3 font-sans text-sm tracking-wide transition hover:bg-memorial-ink hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
            >
              Дізнатися більше
            </a>
          </div>
        </div>
      </section>

      <section id="about" class="mx-auto max-w-3xl px-6 py-20 md:py-28" appReveal="fade-up">
        <h2 class="font-serif text-3xl md:text-4xl">Про проєкт</h2>
        <p class="mt-6 font-sans text-lg leading-relaxed text-memorial-ink/90">
          Nezabuti зберігає історії, світлини й спогади про тих, кого не можна забути.
          Кожна сторінка має постійне посилання та QR-код для фізичних місць пам’яті.
        </p>
      </section>

      <section class="border-y border-memorial-line bg-memorial-surface px-6 py-20 md:py-28" appReveal="fade-up">
        <div class="mx-auto max-w-3xl">
          <h2 class="font-serif text-3xl md:text-4xl">Як це працює</h2>
          <ol class="mt-8 space-y-6 font-sans text-lg leading-relaxed">
            <li>1. Створюється меморіальна сторінка з іменем і світлиною.</li>
            <li>2. Додаються блоки: біографія, служба, нагороди, спогади, галерея.</li>
            <li>3. Сторінка публікується за постійним URL і QR-кодом.</li>
          </ol>
        </div>
      </section>

      <section class="mx-auto max-w-3xl px-6 py-20 md:py-28" appReveal="fade-up">
        <h2 class="font-serif text-3xl md:text-4xl">Для чого потрібен цифровий меморіал</h2>
        <p class="mt-6 font-sans text-lg leading-relaxed text-memorial-ink/90">
          Щоб пам’ять була доступною родині, побратимам і наступним поколінням —
          у зручному форматі, з повагою до історії людини.
        </p>
      </section>

      <section id="contacts" class="border-t border-memorial-line bg-memorial-surface px-6 py-20 md:py-28" appReveal="fade-up">
        <div class="mx-auto max-w-3xl">
          <h2 class="font-serif text-3xl md:text-4xl">Контакти</h2>
          <p class="mt-5 max-w-xl font-sans text-lg leading-relaxed text-memorial-ink/90">
            Ми на зв’язку, якщо потрібна консультація щодо створення меморіальної сторінки або QR-таблички.
          </p>

          @if (phoneHref || telegramUrl || viberHref) {
            <ul class="mt-10 space-y-5 font-sans">
              @if (phoneHref && contacts.phone) {
                <li>
                  <a
                    [href]="phoneHref"
                    class="group inline-flex items-center gap-3 text-memorial-ink transition hover:text-memorial-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
                  >
                    <span
                      class="flex h-10 w-10 shrink-0 items-center justify-center border border-memorial-line bg-memorial-bg text-memorial-muted"
                      aria-hidden="true"
                    >
                      <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          d="M6.6 4.8c.4-.4 1-.5 1.5-.3l2.3 1c.5.2.8.7.7 1.2l-.4 2.1c-.1.4.1.8.4 1.1l2.1 2.1c.3.3.7.5 1.1.4l2.1-.4c.5-.1 1 .2 1.2.7l1 2.3c.2.5.1 1.1-.3 1.5l-1.2 1.2c-.4.4-1 .6-1.6.5-2.2-.4-4.7-1.9-6.9-4.1S5.7 10.3 5.3 8.1c-.1-.6.1-1.2.5-1.6L6.6 4.8Z"
                        />
                      </svg>
                    </span>
                    <span>
                      <span class="block text-xs uppercase tracking-[0.16em] text-memorial-muted">Телефон</span>
                      <span class="mt-0.5 block text-lg">{{ contacts.phone }}</span>
                    </span>
                  </a>
                </li>
              }

              @if (telegramUrl && contacts.telegram) {
                <li>
                  <a
                    [href]="telegramUrl"
                    target="_blank"
                    rel="noopener noreferrer"
                    class="group inline-flex items-center gap-3 text-memorial-ink transition hover:text-memorial-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
                  >
                    <span
                      class="flex h-10 w-10 shrink-0 items-center justify-center border border-memorial-line bg-memorial-bg text-memorial-muted"
                      aria-hidden="true"
                    >
                      <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                        <path
                          d="M21.5 4.3 3.7 11.1c-1.2.5-1.2 1.1-.2 1.4l4.6 1.4 1.8 5.4c.2.7.1.9.8.9.5 0 .7-.2 1-.5l2.3-2.2 4.8 3.5c.9.5 1.5.2 1.7-.8L22.9 5.5c.3-1.2-.4-1.7-1.4-1.2ZM9.7 14.3l-.3 3.3 1.5-2.1 7.2-6.5c.3-.3.1-.4-.2-.2l-8.2 5.5Z"
                        />
                      </svg>
                    </span>
                    <span>
                      <span class="block text-xs uppercase tracking-[0.16em] text-memorial-muted">Telegram</span>
                      <span class="mt-0.5 block text-lg">{{ tgLabel }}</span>
                    </span>
                  </a>
                </li>
              }

              @if (viberHref && contacts.viber) {
                <li>
                  <a
                    [href]="viberHref"
                    class="group inline-flex items-center gap-3 text-memorial-ink transition hover:text-memorial-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
                  >
                    <span
                      class="flex h-10 w-10 shrink-0 items-center justify-center border border-memorial-line bg-memorial-bg text-memorial-muted"
                      aria-hidden="true"
                    >
                      <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                        <path
                          d="M12.2 3C7.5 3 3.7 6.3 3.7 10.4c0 2.3 1.2 4.4 3.1 5.8v2.8l2.9-1.6c.8.2 1.6.3 2.5.3 4.7 0 8.5-3.3 8.5-7.3S16.9 3 12.2 3Zm4.7 9.5-1.5.9c-.2.1-.4.1-.6 0l-1.7-1.1c-.2-.1-.3-.4-.2-.6l.5-1.3c.1-.2 0-.4-.2-.5L11.8 9c-.2-.1-.4 0-.5.2l-.6 1.3c-1-.8-1.7-1.8-2.1-2.9l1.2-.8c.2-.1.3-.3.2-.5L9.5 4.9c-.1-.2-.3-.3-.5-.2L7.5 5.4c-.2.1-.3.3-.3.5.5 3.1 2.5 5.8 5.1 7.4.2.1.4.1.6 0l1.6-.9c.2-.1.3-.3.2-.5l-.5-1.3c-.1-.2 0-.4.2-.5l1.4-.9c.2-.1.4 0 .5.2l.9 1.4c.1.2.1.4-.1.5Z"
                        />
                      </svg>
                    </span>
                    <span>
                      <span class="block text-xs uppercase tracking-[0.16em] text-memorial-muted">Viber</span>
                      <span class="mt-0.5 block text-lg">{{ contacts.viber }}</span>
                    </span>
                  </a>
                </li>
              }
            </ul>
          }
        </div>
      </section>

      <footer class="px-6 py-10 text-center font-sans text-sm text-memorial-muted">
        <p>© Nezabuti</p>
      </footer>
    </div>
  `
})
export class HomePageComponent implements OnInit {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly canonical = inject(CanonicalService);
  private readonly document = inject(DOCUMENT);
  private readonly api = inject(ApiService);

  contacts: SiteSettings = { phone: '', telegram: '', viber: '' };
  phoneHref: string | null = null;
  telegramUrl: string | null = null;
  viberHref: string | null = null;
  tgLabel = '';

  ngOnInit(): void {
    const origin = this.document.location?.origin || 'http://localhost:8088';
    this.title.setTitle('Nezabuti');
    this.meta.updateTag({ name: 'description', content: 'Цифрові меморіальні сторінки про полеглих.' });
    this.meta.updateTag({ name: 'robots', content: 'index,follow' });
    this.canonical.set(`${origin}/`);

    this.api.getPublicSettings().subscribe({
      next: (s) => this.applyContacts(s),
      error: () => this.applyContacts({ phone: '', telegram: '', viber: '' })
    });
  }

  private applyContacts(s: SiteSettings): void {
    this.contacts = {
      phone: (s.phone ?? '').trim(),
      telegram: (s.telegram ?? '').trim(),
      viber: (s.viber ?? '').trim()
    };
    this.phoneHref = phoneTelHref(this.contacts.phone);
    this.telegramUrl = telegramHref(this.contacts.telegram);
    this.viberHref = viberChatLink(this.contacts.viber);
    this.tgLabel = telegramLabel(this.contacts.telegram);
  }
}
