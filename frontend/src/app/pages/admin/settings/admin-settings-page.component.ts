import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { HomeShowcaseAdmin, HowItWorksSlideAdmin, PhotoRef, SiteSettings } from '../../../core/models/memorial.models';
import { isAllowedDemoUrl } from '../../../core/utils/demo-url';

const MAX_HOME_SLIDES = 12;

function emptyHomeShowcase(): HomeShowcaseAdmin {
  return {
    howItWorksEnabled: true,
    howItWorksSlides: [],
    demoEnabled: false,
    demoTitle: '',
    demoDescription: '',
    demoUrl: '',
    demoPreviewImage: null
  };
}

const EMPTY_SETTINGS: SiteSettings = {
  phone: '',
  telegram: '',
  viber: '',
  additionalUpdatePrice: 250,
  qrSize50PriceDelta: 0,
  qrSize75PriceDelta: 100,
  qrSize100PriceDelta: 200,
  telegramNotifyEnabled: false,
  telegramBotTokenMasked: '',
  hasTelegramBotToken: false,
  telegramChatId: '',
  shortTextMaxChars: 2000,
  textBlockMaxChars: 20000,
  quoteMaxChars: 1000,
  timelineDescriptionMaxChars: 5000,
  memoryTextMaxChars: 10000,
  serviceDescriptionMaxChars: 10000,
  awardDescriptionMaxChars: 5000,
  photoCaptionMaxChars: 1000,
  homeShowcase: emptyHomeShowcase()
};

@Component({
  selector: 'app-admin-settings-page',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="space-y-8">
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 class="font-serif text-3xl">Налаштування</h1>
          <p class="mt-2 font-sans text-sm text-memorial-muted">Головна сторінка, контакти, комерційні доплати та технічні ліміти тексту.</p>
        </div>
        @if (!loading) {
          <button type="button" class="bg-memorial-ink px-4 py-2 font-sans text-sm text-white disabled:opacity-50" [disabled]="saving" (click)="save()">
            {{ saving ? 'Збереження…' : 'Зберегти' }}
          </button>
        }
      </div>

      @if (loading) {
        <p class="font-sans text-sm text-memorial-muted" aria-busy="true">Завантаження…</p>
      } @else {
        @if (message) {
          <p class="font-sans text-sm text-memorial-accent">{{ message }}</p>
        }
        @if (error) {
          <p class="font-sans text-sm text-red-700">{{ error }}</p>
        }

        <nav class="flex flex-wrap gap-1 border-b border-memorial-line font-sans text-sm" aria-label="Розділи налаштувань">
          <button
            type="button"
            class="border-b-2 px-4 py-2.5 transition"
            [class.border-memorial-ink]="settingsTab === 'home'"
            [class.text-memorial-ink]="settingsTab === 'home'"
            [class.border-transparent]="settingsTab !== 'home'"
            [class.text-memorial-muted]="settingsTab !== 'home'"
            (click)="settingsTab = 'home'"
          >
            Головна сторінка
          </button>
          <button
            type="button"
            class="border-b-2 px-4 py-2.5 transition"
            [class.border-memorial-ink]="settingsTab === 'contacts'"
            [class.text-memorial-ink]="settingsTab === 'contacts'"
            [class.border-transparent]="settingsTab !== 'contacts'"
            [class.text-memorial-muted]="settingsTab !== 'contacts'"
            (click)="settingsTab = 'contacts'"
          >
            Контакти
          </button>
          <button
            type="button"
            class="border-b-2 px-4 py-2.5 transition"
            [class.border-memorial-ink]="settingsTab === 'telegram'"
            [class.text-memorial-ink]="settingsTab === 'telegram'"
            [class.border-transparent]="settingsTab !== 'telegram'"
            [class.text-memorial-muted]="settingsTab !== 'telegram'"
            (click)="settingsTab = 'telegram'"
          >
            Telegram-сповіщення
          </button>
          <button
            type="button"
            class="border-b-2 px-4 py-2.5 transition"
            [class.border-memorial-ink]="settingsTab === 'commerce'"
            [class.text-memorial-ink]="settingsTab === 'commerce'"
            [class.border-transparent]="settingsTab !== 'commerce'"
            [class.text-memorial-muted]="settingsTab !== 'commerce'"
            (click)="settingsTab = 'commerce'"
          >
            Комерція
          </button>
          <button
            type="button"
            class="border-b-2 px-4 py-2.5 transition"
            [class.border-memorial-ink]="settingsTab === 'limits'"
            [class.text-memorial-ink]="settingsTab === 'limits'"
            [class.border-transparent]="settingsTab !== 'limits'"
            [class.text-memorial-muted]="settingsTab !== 'limits'"
            (click)="settingsTab = 'limits'"
          >
            Технічні ліміти символів
          </button>
        </nav>

        @if (settingsTab === 'home') {
        <section class="max-w-3xl space-y-6 border border-memorial-line bg-white p-6">
          <p class="font-sans text-sm text-memorial-muted">Презентація «Як це працює» та посилання на демо-сторінку.</p>

          <div class="space-y-4 border-t border-memorial-line pt-5">
            <h3 class="font-serif text-lg">Як це працює</h3>
            <label class="flex items-center gap-2 font-sans text-sm">
              <input type="checkbox" [(ngModel)]="showcase.howItWorksEnabled" name="howEnabled" [disabled]="saving" />
              Показати блок на головній
            </label>

            <div class="space-y-4">
              @for (slide of showcase.howItWorksSlides; track $index; let i = $index) {
                <article class="border border-memorial-line p-4">
                  <div class="flex flex-wrap items-center justify-between gap-2">
                    <p class="font-sans text-sm text-memorial-muted">Слайд {{ i + 1 }}</p>
                    <div class="flex flex-wrap gap-2">
                      <button type="button" class="border border-memorial-line px-2 py-1 font-sans text-xs" [disabled]="i === 0 || saving" (click)="moveSlide(i, -1)">Вгору</button>
                      <button type="button" class="border border-memorial-line px-2 py-1 font-sans text-xs" [disabled]="i === showcase.howItWorksSlides.length - 1 || saving" (click)="moveSlide(i, 1)">Вниз</button>
                      <button type="button" class="border border-memorial-line px-2 py-1 font-sans text-xs text-memorial-muted" [disabled]="saving" (click)="removeSlide(i)">Видалити</button>
                    </div>
                  </div>
                  <label class="mt-3 flex items-center gap-2 font-sans text-sm">
                    <input type="checkbox" [(ngModel)]="slide.enabled" name="slideEnabled{{ i }}" [disabled]="saving" />
                    Увімкнено
                  </label>
                  <label class="mt-3 block font-sans text-sm">
                    Заголовок
                    <input class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="slide.title" name="slideTitle{{ i }}" [disabled]="saving" />
                  </label>
                  <label class="mt-3 block font-sans text-sm">
                    Опис
                    <textarea class="mt-1 w-full border border-memorial-line px-3 py-2" rows="2" [(ngModel)]="slide.description" name="slideDesc{{ i }}" [disabled]="saving"></textarea>
                  </label>
                  <label class="mt-3 block font-sans text-sm">
                    Alt-текст (необов’язково)
                    <input class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="slide.altText" name="slideAlt{{ i }}" [disabled]="saving" />
                  </label>
                  <div class="mt-4 grid gap-4 md:grid-cols-2">
                    <div class="font-sans text-sm">
                      <p>Зображення для десктопа</p>
                      <p class="text-xs text-memorial-muted">Рекомендовано 16:9</p>
                      @if (slideDesktop(slide)?.thumbUrl || slideDesktop(slide)?.previewUrl) {
                        <img [src]="slideDesktop(slide)?.thumbUrl || slideDesktop(slide)?.previewUrl" [alt]="slide.altText || slide.title || 'Десктоп'" class="mt-2 max-h-28 object-contain" />
                        <button type="button" class="mt-2 border border-memorial-line px-2 py-1 text-xs" [disabled]="saving" (click)="clearSlideImage(i, 'desktop')">Прибрати</button>
                      }
                      <input class="mt-2 block w-full text-sm" type="file" accept="image/jpeg,image/png,image/webp,image/gif" [disabled]="saving || uploadingSlideKey === i + '-desktop'" (change)="uploadSlideImage(i, 'desktop', $event)" />
                    </div>
                    <div class="font-sans text-sm">
                      <p>Зображення для мобільних</p>
                      <p class="text-xs text-memorial-muted">Рекомендовано 4:5, необов’язково</p>
                      @if (slide.mobileImage?.thumbUrl || slide.mobileImage?.previewUrl) {
                        <img [src]="slide.mobileImage?.thumbUrl || slide.mobileImage?.previewUrl" [alt]="slide.altText || slide.title || 'Мобільне'" class="mt-2 max-h-28 object-contain" />
                        <button type="button" class="mt-2 border border-memorial-line px-2 py-1 text-xs" [disabled]="saving" (click)="clearSlideImage(i, 'mobile')">Прибрати</button>
                      }
                      <input class="mt-2 block w-full text-sm" type="file" accept="image/jpeg,image/png,image/webp,image/gif" [disabled]="saving || uploadingSlideKey === i + '-mobile'" (change)="uploadSlideImage(i, 'mobile', $event)" />
                    </div>
                  </div>
                </article>
              }
            </div>

            <button
              type="button"
              class="border border-memorial-ink px-4 py-2 font-sans text-sm disabled:opacity-50"
              [disabled]="saving || showcase.howItWorksSlides.length >= maxSlides"
              (click)="addSlide()"
            >
              Додати слайд
            </button>
          </div>

          <div class="space-y-4 border-t border-memorial-line pt-5">
            <h3 class="font-serif text-lg">Демо-сторінка</h3>
            <label class="flex items-center gap-2 font-sans text-sm">
              <input type="checkbox" [(ngModel)]="showcase.demoEnabled" name="demoEnabled" [disabled]="saving" />
              Показати заклик на головній
            </label>
            <label class="block font-sans text-sm">
              Заголовок
              <input class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="showcase.demoTitle" name="demoTitle" placeholder="Подивіться, як виглядає готова сторінка" [disabled]="saving" />
            </label>
            <label class="block font-sans text-sm">
              Опис
              <textarea class="mt-1 w-full border border-memorial-line px-3 py-2" rows="2" [(ngModel)]="showcase.demoDescription" name="demoDescription" [disabled]="saving"></textarea>
            </label>
            <label class="block font-sans text-sm">
              URL
              <input class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="showcase.demoUrl" name="demoUrl" placeholder="/m/DEMO1234AB або https://…" [disabled]="saving" />
            </label>
            <div class="font-sans text-sm">
              <p>Прев’ю (необов’язково)</p>
              @if (showcase.demoPreviewImage?.thumbUrl || showcase.demoPreviewImage?.previewUrl) {
                <img [src]="showcase.demoPreviewImage?.thumbUrl || showcase.demoPreviewImage?.previewUrl" [alt]="showcase.demoTitle || 'Прев’ю демо-сторінки'" class="mt-2 max-h-28 object-contain" />
              }
              <input class="mt-2 block w-full text-sm" type="file" accept="image/jpeg,image/png,image/webp,image/gif" [disabled]="saving || uploadingDemo" (change)="uploadDemoImage($event)" />
            </div>
          </div>
        </section>
        }

        @if (settingsTab === 'contacts') {
        <section class="max-w-2xl space-y-5 border border-memorial-line bg-white p-6">
          <label class="block font-sans text-sm">
            Телефон
            <input class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.phone" name="phone" autocomplete="tel" placeholder="+380…" [disabled]="saving" />
          </label>
          <label class="block font-sans text-sm">
            Telegram
            <input class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.telegram" name="telegram" placeholder="@username або https://t.me/…" [disabled]="saving" />
          </label>
          <label class="block font-sans text-sm">
            Viber
            <input class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.viber" name="viber" placeholder="+380…" [disabled]="saving" />
          </label>
        </section>
        }

        @if (settingsTab === 'telegram') {
        <section class="max-w-2xl space-y-5 border border-memorial-line bg-white p-6">
          <p class="font-sans text-sm text-memorial-muted">
            Службові повідомлення адміну про закінчення оплати та призупинення сторінок.
            Тестове повідомлення надсилається у форматі реального нагадування на основі меморіалу YSQG27AFFT.
          </p>
          <label class="flex items-center gap-2 font-sans text-sm">
            <input type="checkbox" [(ngModel)]="form.telegramNotifyEnabled" name="tgEnabled" [disabled]="saving" />
            Увімкнено
          </label>
          <label class="block font-sans text-sm">
            Bot token
            <input
              class="mt-1 w-full border border-memorial-line px-3 py-2"
              [(ngModel)]="telegramBotTokenDraft"
              name="tgToken"
              type="password"
              autocomplete="off"
              [placeholder]="tokenPlaceholder"
              [disabled]="saving"
            />
            @if (form.hasTelegramBotToken && form.telegramBotTokenMasked) {
              <span class="mt-1 block text-xs text-memorial-muted">Збережено: {{ form.telegramBotTokenMasked }}</span>
            }
          </label>
          <label class="block font-sans text-sm">
            Chat ID
            <input
              class="mt-1 w-full border border-memorial-line px-3 py-2"
              [(ngModel)]="form.telegramChatId"
              name="tgChat"
              placeholder="напр. -100…"
              [disabled]="saving"
            />
          </label>
          <div class="flex flex-wrap items-center gap-3">
            <button
              type="button"
              class="border border-memorial-ink px-4 py-2 font-sans text-sm disabled:opacity-50"
              [disabled]="testingTelegram || saving"
              (click)="testTelegram()"
            >
              {{ testingTelegram ? 'Перевірка…' : 'Надіслати тестове повідомлення' }}
            </button>
            @if (telegramTestMessage) {
              <span class="font-sans text-sm" [class.text-memorial-accent]="telegramTestOk" [class.text-red-700]="!telegramTestOk">
                {{ telegramTestMessage }}
              </span>
            }
          </div>
        </section>
        }

        @if (settingsTab === 'commerce') {
        <section class="max-w-2xl space-y-5 border border-memorial-line bg-white p-6">
          <label class="block font-sans text-sm">
            Ціна додаткового оновлення (грн)
            <input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.additionalUpdatePrice" name="aup" [disabled]="saving" />
          </label>
          <div class="grid gap-3 sm:grid-cols-3">
            <label class="block font-sans text-sm">
              QR 50 мм (+грн)
              <input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.qrSize50PriceDelta" name="qr50" [disabled]="saving" />
            </label>
            <label class="block font-sans text-sm">
              QR 75 мм (+грн)
              <input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.qrSize75PriceDelta" name="qr75" [disabled]="saving" />
            </label>
            <label class="block font-sans text-sm">
              QR 100 мм (+грн)
              <input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.qrSize100PriceDelta" name="qr100" [disabled]="saving" />
            </label>
          </div>
        </section>
        }

        @if (settingsTab === 'limits') {
        <section class="max-w-2xl space-y-5 border border-memorial-line bg-white p-6">
          <div class="grid gap-3 sm:grid-cols-2">
            <label class="block font-sans text-sm">Короткий опис<input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.shortTextMaxChars" name="st" [disabled]="saving" /></label>
            <label class="block font-sans text-sm">Текстовий блок<input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.textBlockMaxChars" name="tb" [disabled]="saving" /></label>
            <label class="block font-sans text-sm">Цитата<input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.quoteMaxChars" name="q" [disabled]="saving" /></label>
            <label class="block font-sans text-sm">Опис події<input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.timelineDescriptionMaxChars" name="td" [disabled]="saving" /></label>
            <label class="block font-sans text-sm">Спогад<input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.memoryTextMaxChars" name="mt" [disabled]="saving" /></label>
            <label class="block font-sans text-sm">Опис служби<input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.serviceDescriptionMaxChars" name="sd" [disabled]="saving" /></label>
            <label class="block font-sans text-sm">Опис відзнаки<input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.awardDescriptionMaxChars" name="ad" [disabled]="saving" /></label>
            <label class="block font-sans text-sm">Підпис фото<input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="form.photoCaptionMaxChars" name="pc" [disabled]="saving" /></label>
          </div>
        </section>
        }
      }
    </div>
  `
})
export class AdminSettingsPageComponent implements OnInit {
  private readonly api = inject(ApiService);

  form: SiteSettings = { ...EMPTY_SETTINGS, homeShowcase: emptyHomeShowcase() };
  telegramBotTokenDraft = '';
  loading = true;
  saving = false;
  testingTelegram = false;
  uploadingSlideKey: string | null = null;
  uploadingDemo = false;
  message = '';
  error = '';
  telegramTestMessage = '';
  telegramTestOk = false;
  settingsTab: 'home' | 'contacts' | 'telegram' | 'commerce' | 'limits' = 'home';
  readonly maxSlides = MAX_HOME_SLIDES;

  get showcase(): HomeShowcaseAdmin {
    this.form.homeShowcase ??= emptyHomeShowcase();
    this.form.homeShowcase.howItWorksSlides ??= [];
    return this.form.homeShowcase;
  }

  get tokenPlaceholder(): string {
    if (this.form.hasTelegramBotToken && this.form.telegramBotTokenMasked) {
      return this.form.telegramBotTokenMasked;
    }
    return 'Вставте токен бота';
  }

  ngOnInit(): void {
    this.api.getAdminSettings().subscribe({
      next: (s) => {
        this.applyForm(s);
        this.telegramBotTokenDraft = '';
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.error = 'Не вдалося завантажити налаштування';
      }
    });
  }

  addSlide(): void {
    if (this.showcase.howItWorksSlides.length >= MAX_HOME_SLIDES) {
      return;
    }
    this.showcase.howItWorksSlides = [...this.showcase.howItWorksSlides, this.newSlide(this.showcase.howItWorksSlides.length)];
  }

  removeSlide(index: number): void {
    this.showcase.howItWorksSlides.splice(index, 1);
    this.reindexSlides();
  }

  moveSlide(index: number, delta: number): void {
    const next = index + delta;
    const slides = this.showcase.howItWorksSlides;
    if (next < 0 || next >= slides.length) {
      return;
    }
    const [item] = slides.splice(index, 1);
    slides.splice(next, 0, item);
    this.reindexSlides();
  }

  uploadSlideImage(index: number, kind: 'desktop' | 'mobile', event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    this.uploadingSlideKey = `${index}-${kind}`;
    this.error = '';
    this.api.uploadHomeImage(file).subscribe({
      next: (photo) => {
        const slide = this.showcase.howItWorksSlides[index];
        if (kind === 'mobile') {
          slide.mobileImage = photo;
        } else {
          slide.desktopImage = photo;
          slide.image = photo;
        }
        this.uploadingSlideKey = null;
        input.value = '';
      },
      error: (err) => {
        this.uploadingSlideKey = null;
        this.error = err?.error?.message || 'Не вдалося завантажити зображення';
        input.value = '';
      }
    });
  }

  clearSlideImage(index: number, kind: 'desktop' | 'mobile'): void {
    const slide = this.showcase.howItWorksSlides[index];
    if (kind === 'mobile') {
      slide.mobileImage = null;
    } else {
      slide.desktopImage = null;
      slide.image = null;
    }
  }

  slideDesktop(slide: HowItWorksSlideAdmin): PhotoRef | null {
    return slide.desktopImage || slide.image || null;
  }

  uploadDemoImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    this.uploadingDemo = true;
    this.error = '';
    this.api.uploadHomeImage(file).subscribe({
      next: (photo) => {
        this.showcase.demoPreviewImage = photo;
        this.uploadingDemo = false;
        input.value = '';
      },
      error: (err) => {
        this.uploadingDemo = false;
        this.error = err?.error?.message || 'Не вдалося завантажити зображення';
        input.value = '';
      }
    });
  }

  save(): void {
    if (this.saving) {
      return;
    }
    this.message = '';
    this.error = '';
    if (this.showcase.demoEnabled && !isAllowedDemoUrl(this.showcase.demoUrl)) {
      this.error = 'Вкажіть коректне посилання на демо-сторінку (https://… або /m/…).';
      return;
    }
    this.saving = true;
    this.reindexSlides();
    const body: Partial<SiteSettings> & { telegramBotToken?: string } = { ...this.form, homeShowcase: this.showcase };
    const token = this.telegramBotTokenDraft.trim();
    if (token) {
      body.telegramBotToken = token;
    }
    this.api.updateAdminSettings(body).subscribe({
      next: (s) => {
        this.applyForm(s);
        this.telegramBotTokenDraft = '';
        this.saving = false;
        this.message = 'Збережено';
      },
      error: (err) => {
        this.saving = false;
        this.error = err?.error?.message || 'Не вдалося зберегти налаштування';
      }
    });
  }

  testTelegram(): void {
    if (this.testingTelegram) {
      return;
    }
    this.testingTelegram = true;
    this.telegramTestMessage = '';
    this.api.testTelegramNotify().subscribe({
      next: (r) => {
        this.testingTelegram = false;
        this.telegramTestOk = !!r.ok;
        this.telegramTestMessage = r.message || (r.ok ? 'Надіслано' : 'Помилка');
      },
      error: (err) => {
        this.testingTelegram = false;
        this.telegramTestOk = false;
        this.telegramTestMessage = err?.error?.message || 'Не вдалося надіслати тест';
      }
    });
  }

  private applyForm(s: SiteSettings): void {
    const incoming = s.homeShowcase;
    this.form = {
      ...EMPTY_SETTINGS,
      ...s,
      homeShowcase: {
        ...emptyHomeShowcase(),
        ...incoming,
        howItWorksSlides: (incoming?.howItWorksSlides ?? []).map((slide, index) => ({
          id: slide.id || this.newSlideId(),
          title: slide.title ?? '',
          description: slide.description ?? '',
          shortCaption: slide.shortCaption ?? '',
          image: slide.desktopImage ?? slide.image ?? null,
          desktopImage: slide.desktopImage ?? slide.image ?? null,
          mobileImage: slide.mobileImage ?? null,
          altText: slide.altText ?? '',
          sortOrder: slide.sortOrder ?? index,
          enabled: slide.enabled !== false
        }))
      }
    };
  }

  private newSlide(sortOrder: number): HowItWorksSlideAdmin {
    return {
      id: this.newSlideId(),
      title: '',
      description: '',
      shortCaption: '',
      image: null,
      desktopImage: null,
      mobileImage: null,
      altText: '',
      sortOrder,
      enabled: true
    };
  }

  private reindexSlides(): void {
    this.showcase.howItWorksSlides.forEach((slide, index) => {
      slide.sortOrder = index;
    });
  }

  private newSlideId(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return crypto.randomUUID();
    }
    return `slide-${Date.now()}-${Math.random().toString(16).slice(2)}`;
  }
}
