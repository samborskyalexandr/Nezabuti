import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { SiteSettings } from '../../../core/models/memorial.models';

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
  photoCaptionMaxChars: 1000
};

@Component({
  selector: 'app-admin-settings-page',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="space-y-8">
      <div>
        <h1 class="font-serif text-3xl">Налаштування</h1>
        <p class="mt-2 font-sans text-sm text-memorial-muted">Контакти, комерційні доплати та технічні ліміти тексту.</p>
      </div>

      @if (loading) {
        <p class="font-sans text-sm text-memorial-muted" aria-busy="true">Завантаження…</p>
      } @else {
        <section class="max-w-2xl space-y-5 border border-memorial-line bg-white p-6">
          <h2 class="font-serif text-xl">Контакти</h2>

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

        <section class="max-w-2xl space-y-5 border border-memorial-line bg-white p-6">
          <h2 class="font-serif text-xl">Telegram-сповіщення</h2>
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

        <section class="max-w-2xl space-y-5 border border-memorial-line bg-white p-6">
          <h2 class="font-serif text-xl">Комерція</h2>
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

        <section class="max-w-2xl space-y-5 border border-memorial-line bg-white p-6">
          <h2 class="font-serif text-xl">Технічні ліміти символів</h2>
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

        @if (message) {
          <p class="font-sans text-sm text-memorial-accent">{{ message }}</p>
        }
        @if (error) {
          <p class="font-sans text-sm text-red-700">{{ error }}</p>
        }

        <button type="button" class="bg-memorial-ink px-4 py-2 font-sans text-sm text-white disabled:opacity-50" [disabled]="saving" (click)="save()">
          {{ saving ? 'Збереження…' : 'Зберегти' }}
        </button>
      }
    </div>
  `
})
export class AdminSettingsPageComponent implements OnInit {
  private readonly api = inject(ApiService);

  form: SiteSettings = { ...EMPTY_SETTINGS };
  telegramBotTokenDraft = '';
  loading = true;
  saving = false;
  testingTelegram = false;
  message = '';
  error = '';
  telegramTestMessage = '';
  telegramTestOk = false;

  get tokenPlaceholder(): string {
    if (this.form.hasTelegramBotToken && this.form.telegramBotTokenMasked) {
      return this.form.telegramBotTokenMasked;
    }
    return 'Вставте токен бота';
  }

  ngOnInit(): void {
    this.api.getAdminSettings().subscribe({
      next: (s) => {
        this.form = { ...EMPTY_SETTINGS, ...s };
        this.telegramBotTokenDraft = '';
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.error = 'Не вдалося завантажити налаштування';
      }
    });
  }

  save(): void {
    if (this.saving) {
      return;
    }
    this.saving = true;
    this.message = '';
    this.error = '';
    const body: Partial<SiteSettings> & { telegramBotToken?: string } = { ...this.form };
    const token = this.telegramBotTokenDraft.trim();
    if (token) {
      body.telegramBotToken = token;
    }
    this.api.updateAdminSettings(body).subscribe({
      next: (s) => {
        this.form = { ...EMPTY_SETTINGS, ...s };
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
}
