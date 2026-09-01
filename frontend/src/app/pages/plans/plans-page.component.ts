import { DOCUMENT, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { RevealDirective } from '../../shared/directives/reveal.directive';
import { CanonicalService } from '../../core/services/canonical.service';
import { ApiService } from '../../core/services/api.service';
import { PublicPlan } from '../../core/models/memorial.models';
import { phoneTelHref, telegramHref, viberChatLink } from '../../core/config/site-contacts';

@Component({
  selector: 'app-plans-page',
  standalone: true,
  imports: [RouterLink, RevealDirective, DecimalPipe],
  template: `
    <div class="min-h-screen bg-memorial-bg">
      <header class="border-b border-memorial-line bg-memorial-surface px-6 py-6 md:px-10">
        <div class="mx-auto flex max-w-6xl items-center justify-between gap-6">
          <a routerLink="/" class="font-serif text-2xl tracking-wide text-memorial-ink">Nezabuti</a>
          <nav class="flex gap-5 font-sans text-sm text-memorial-muted">
            <a routerLink="/" class="transition hover:text-memorial-ink">Головна</a>
            <a href="#contacts" class="transition hover:text-memorial-ink">Контакти</a>
          </nav>
        </div>
      </header>

      <section class="px-6 pb-10 pt-16 md:px-10 md:pb-14 md:pt-24" appReveal="fade-in">
        <div class="mx-auto max-w-3xl text-center">
          <h1 class="font-serif text-4xl font-semibold md:text-5xl">Тарифні плани</h1>
          <p class="mt-5 font-sans text-lg leading-relaxed text-memorial-muted">
            Оберіть формат сторінки пам’яті. Кожен план — одноразова оплата створення.
            Сторінка зберігається назавжди.
          </p>
        </div>
      </section>

      <section class="mx-auto max-w-6xl px-6 pb-20 md:px-10 md:pb-28">
        @if (loading) {
          <p class="text-center font-sans text-memorial-muted">Завантаження…</p>
        } @else {
          <div class="grid gap-8 md:grid-cols-3">
            @for (plan of plans; track plan.code) {
              <article
                class="flex flex-col border border-memorial-line bg-memorial-surface p-8"
                [class.ring-1]="plan.isRecommended"
                [class.ring-memorial-ink]="plan.isRecommended"
                appReveal="fade-up"
              >
                @if (plan.isRecommended) {
                  <p class="mb-3 font-sans text-xs uppercase tracking-[0.18em] text-memorial-accent">Рекомендований</p>
                }
                <h2 class="font-serif text-3xl">{{ plan.name }}</h2>
                <p class="mt-4 font-serif text-4xl font-semibold">{{ plan.price | number:'1.0-0' }} ₴</p>
                <p class="mt-4 flex-1 font-sans text-sm leading-relaxed text-memorial-muted">
                  {{ plan.description || marketingCopy(plan.code) }}
                </p>
                <ul class="mt-6 space-y-2 font-sans text-sm text-memorial-ink/90">
                  @if (plan.maxGalleryBlocks != null) {
                    <li>Галерей: до {{ plan.maxGalleryBlocks }}</li>
                  }
                  @if (plan.maxPhotosPerGallery != null) {
                    <li>Фото в галереї: до {{ plan.maxPhotosPerGallery }}</li>
                  }
                  @if (plan.maxTimelineEvents != null) {
                    <li>Подій життєвого шляху: до {{ plan.maxTimelineEvents }}</li>
                  }
                  @if (plan.maxMemories != null) {
                    <li>Спогадів: до {{ plan.maxMemories }}</li>
                  }
                  <li>Оновлень включено: {{ plan.includedUpdates }}</li>
                </ul>
              </article>
            }
          </div>

          <p class="mx-auto mt-14 max-w-2xl text-center font-serif text-xl italic text-memorial-ink/80" appReveal="fade-up">
            Усі опубліковані меморіальні сторінки залишаються доступними безстроково. Щомісячної або щорічної абонплати немає.
          </p>

          <div id="contacts" class="mx-auto mt-16 max-w-xl text-center" appReveal="fade-up">
            <h2 class="font-serif text-2xl">Замовити</h2>
            <p class="mt-3 font-sans text-memorial-muted">
              Напишіть нам — допоможемо обрати план і підготувати сторінку.
            </p>
            <div class="mt-6 flex flex-wrap justify-center gap-4 font-sans text-sm">
              @if (phoneHref) {
                <a [href]="phoneHref" class="border border-memorial-ink px-4 py-2 transition hover:bg-memorial-ink hover:text-white">Телефон</a>
              }
              @if (telegramUrl) {
                <a [href]="telegramUrl" target="_blank" rel="noopener" class="border border-memorial-ink px-4 py-2 transition hover:bg-memorial-ink hover:text-white">Telegram</a>
              }
              @if (viberHref) {
                <a [href]="viberHref" class="border border-memorial-ink px-4 py-2 transition hover:bg-memorial-ink hover:text-white">Viber</a>
              }
            </div>
          </div>
        }
      </section>
    </div>
  `
})
export class PlansPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly canonical = inject(CanonicalService);
  private readonly document = inject(DOCUMENT);

  plans: PublicPlan[] = [];
  loading = true;
  phoneHref: string | null = null;
  telegramUrl: string | null = null;
  viberHref: string | null = null;

  ngOnInit(): void {
    this.title.setTitle('Тарифні плани — Nezabuti');
    this.meta.updateTag({
      name: 'description',
      content: 'Тарифні плани цифрових меморіальних сторінок Nezabuti. Сторінка зберігається назавжди.'
    });
    this.canonical.set(`${this.document.defaultView?.location?.origin || ''}/plans`);

    this.api.getPublicPlans().subscribe({
      next: (plans) => {
        this.plans = plans;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });

    this.api.getPublicSettings().subscribe({
      next: (s) => {
        this.phoneHref = phoneTelHref(s.phone);
        this.telegramUrl = telegramHref(s.telegram);
        this.viberHref = viberChatLink(s.viber);
      }
    });
  }

  marketingCopy(code: string): string {
    switch (code) {
      case 'Memory':
        return 'Лаконічна сторінка з головною історією та світлинами.';
      case 'Story':
        return 'Розгорнута історія життя з галереями, шляхом і спогадами.';
      case 'Legacy':
        return 'Повний цифровий меморіал для великої родини і багатьох матеріалів.';
      default:
        return '';
    }
  }
}
