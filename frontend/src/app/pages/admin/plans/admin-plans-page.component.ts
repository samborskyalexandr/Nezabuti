import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { Plan } from '../../../core/models/memorial.models';

@Component({
  selector: 'app-admin-plans-page',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="space-y-8">
      <div>
        <h1 class="font-serif text-3xl">Плани</h1>
        <p class="mt-2 font-sans text-sm text-memorial-muted">
          Редагування цін і лімітів тарифів. Стандартні плани не видаляються.
        </p>
      </div>

      @if (loading) {
        <p class="font-sans text-sm text-memorial-muted">Завантаження…</p>
      } @else {
        <div class="grid gap-6 lg:grid-cols-2">
          @for (plan of plans; track plan.id) {
            <section class="space-y-4 border border-memorial-line bg-white p-6">
              <div class="flex flex-wrap items-baseline justify-between gap-2">
                <h2 class="font-serif text-2xl">{{ plan.name }}</h2>
                <span class="font-sans text-xs uppercase tracking-wide text-memorial-muted">{{ plan.code }}</span>
              </div>

              <label class="block font-sans text-sm">
                Назва
                <input class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="plan.name" [name]="'name-' + plan.id" />
              </label>

              <label class="block font-sans text-sm">
                Опис
                <textarea
                  class="mt-1 w-full border border-memorial-line px-3 py-2"
                  rows="2"
                  [(ngModel)]="plan.description"
                  [name]="'desc-' + plan.id"
                ></textarea>
              </label>

              <div class="grid gap-3 sm:grid-cols-2">
                <label class="block font-sans text-sm">
                  Ціна (грн)
                  <input
                    type="number"
                    class="mt-1 w-full border border-memorial-line px-3 py-2"
                    [(ngModel)]="plan.price"
                    [name]="'price-' + plan.id"
                  />
                </label>
                <label class="block font-sans text-sm">
                  Включені оновлення
                  <input
                    type="number"
                    class="mt-1 w-full border border-memorial-line px-3 py-2"
                    [(ngModel)]="plan.includedUpdates"
                    [name]="'upd-' + plan.id"
                  />
                </label>
              </div>

              <label class="flex items-center gap-2 font-sans text-sm">
                <input type="checkbox" [(ngModel)]="plan.isActive" [name]="'active-' + plan.id" />
                Активний
              </label>

              @if (plan.isCustom) {
                <label class="flex items-center gap-2 font-sans text-sm">
                  <input type="checkbox" [(ngModel)]="plan.isUnlimited" [name]="'unlim-' + plan.id" />
                  Безлімітний за замовчуванням
                </label>
              }

              @if (!plan.isUnlimited) {
                <div class="grid gap-3 sm:grid-cols-2">
                  <label class="block font-sans text-sm">
                    Макс. блоків
                    <input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="plan.maxBlocks" [name]="'mb-' + plan.id" />
                  </label>
                  <label class="block font-sans text-sm">
                    Макс. галерей
                    <input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="plan.maxGalleryBlocks" [name]="'mg-' + plan.id" />
                  </label>
                  <label class="block font-sans text-sm">
                    Фото в галереї
                    <input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="plan.maxPhotosPerGallery" [name]="'mp-' + plan.id" />
                  </label>
                  <label class="block font-sans text-sm">
                    Події шляху
                    <input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="plan.maxTimelineEvents" [name]="'mt-' + plan.id" />
                  </label>
                  <label class="block font-sans text-sm">
                    Спогади
                    <input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="plan.maxMemories" [name]="'mm-' + plan.id" />
                  </label>
                </div>
              }

              @if (messages[plan.id]) {
                <p class="font-sans text-sm text-memorial-accent">{{ messages[plan.id] }}</p>
              }
              @if (errors[plan.id]) {
                <p class="font-sans text-sm text-red-700">{{ errors[plan.id] }}</p>
              }

              <button
                type="button"
                class="bg-memorial-ink px-4 py-2 font-sans text-sm text-white disabled:opacity-50"
                [disabled]="savingId === plan.id"
                (click)="save(plan)"
              >
                {{ savingId === plan.id ? 'Збереження…' : 'Зберегти' }}
              </button>
            </section>
          }
        </div>
      }
    </div>
  `
})
export class AdminPlansPageComponent implements OnInit {
  private readonly api = inject(ApiService);

  plans: Plan[] = [];
  loading = true;
  savingId: string | null = null;
  messages: Record<string, string> = {};
  errors: Record<string, string> = {};

  ngOnInit(): void {
    this.api.listPlans().subscribe({
      next: (plans) => {
        this.plans = plans;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errors['_'] = 'Не вдалося завантажити плани';
      }
    });
  }

  save(plan: Plan): void {
    this.savingId = plan.id;
    this.messages[plan.id] = '';
    this.errors[plan.id] = '';
    this.api
      .updatePlan(plan.id, {
        name: plan.name,
        description: plan.description,
        price: Number(plan.price) || 0,
        isActive: plan.isActive,
        isUnlimited: plan.isUnlimited,
        maxBlocks: plan.maxBlocks,
        maxGalleryBlocks: plan.maxGalleryBlocks,
        maxPhotosPerGallery: plan.maxPhotosPerGallery,
        maxTimelineEvents: plan.maxTimelineEvents,
        maxMemories: plan.maxMemories,
        includedUpdates: Number(plan.includedUpdates) || 0
      })
      .subscribe({
        next: (updated) => {
          const idx = this.plans.findIndex((p) => p.id === plan.id);
          if (idx >= 0) {
            this.plans[idx] = updated;
          }
          this.savingId = null;
          this.messages[plan.id] = 'Збережено';
        },
        error: (err) => {
          this.savingId = null;
          this.errors[plan.id] = err?.error?.message || 'Не вдалося зберегти план';
        }
      });
  }
}
