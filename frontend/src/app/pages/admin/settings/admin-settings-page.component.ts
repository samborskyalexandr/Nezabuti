import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { SiteSettings } from '../../../core/models/memorial.models';

@Component({
  selector: 'app-admin-settings-page',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="space-y-8">
      <div>
        <h1 class="font-serif text-3xl">Налаштування</h1>
        <p class="mt-2 font-sans text-sm text-memorial-muted">Публічні контакти для головної сторінки.</p>
      </div>

      @if (loading) {
        <p class="font-sans text-sm text-memorial-muted" aria-busy="true">Завантаження…</p>
      } @else {
        <section class="max-w-lg space-y-5 border border-memorial-line bg-white p-6">
          <h2 class="font-serif text-xl">Контакти</h2>

          <label class="block font-sans text-sm">
            Телефон
            <input
              class="mt-1 w-full border border-memorial-line px-3 py-2"
              [(ngModel)]="form.phone"
              name="phone"
              autocomplete="tel"
              placeholder="+380…"
              [disabled]="saving"
            />
          </label>

          <label class="block font-sans text-sm">
            Telegram
            <input
              class="mt-1 w-full border border-memorial-line px-3 py-2"
              [(ngModel)]="form.telegram"
              name="telegram"
              placeholder="@username або https://t.me/…"
              [disabled]="saving"
            />
          </label>

          <label class="block font-sans text-sm">
            Viber
            <input
              class="mt-1 w-full border border-memorial-line px-3 py-2"
              [(ngModel)]="form.viber"
              name="viber"
              placeholder="+380…"
              [disabled]="saving"
            />
          </label>

          @if (message) {
            <p class="font-sans text-sm text-memorial-accent">{{ message }}</p>
          }
          @if (error) {
            <p class="font-sans text-sm text-red-700">{{ error }}</p>
          }

          <button
            type="button"
            class="bg-memorial-ink px-4 py-2 font-sans text-sm text-white disabled:opacity-50"
            [disabled]="saving"
            (click)="save()"
          >
            {{ saving ? 'Збереження…' : 'Зберегти' }}
          </button>
        </section>
      }
    </div>
  `
})
export class AdminSettingsPageComponent implements OnInit {
  private readonly api = inject(ApiService);

  form: SiteSettings = { phone: '', telegram: '', viber: '' };
  loading = true;
  saving = false;
  message = '';
  error = '';

  ngOnInit(): void {
    this.api.getAdminSettings().subscribe({
      next: (s) => {
        this.form = { phone: s.phone ?? '', telegram: s.telegram ?? '', viber: s.viber ?? '' };
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
    this.api
      .updateAdminSettings({
        phone: this.form.phone,
        telegram: this.form.telegram,
        viber: this.form.viber
      })
      .subscribe({
        next: (s) => {
          this.form = { phone: s.phone ?? '', telegram: s.telegram ?? '', viber: s.viber ?? '' };
          this.saving = false;
          this.message = 'Збережено';
        },
        error: (err) => {
          this.saving = false;
          this.error = err?.error?.message || 'Не вдалося зберегти налаштування';
        }
      });
  }
}
