import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import {
  MemorialListItem,
  MemorialStatus,
  PAYMENT_STATUS_LABELS,
  PRIVACY_LABELS,
  Plan,
  STATUS_LABELS
} from '../../../core/models/memorial.models';
import { adminUrl } from '../../../core/config/admin-routes';

@Component({
  selector: 'app-admin-memorial-list-page',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe, DecimalPipe],
  template: `
    <div class="space-y-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <h1 class="font-serif text-3xl">Меморіали</h1>
        <button type="button" class="bg-memorial-ink px-4 py-2 font-sans text-sm text-white" (click)="openCreate()">
          Створити
        </button>
      </div>

      <div class="flex flex-wrap gap-3">
        <input class="border border-memorial-line bg-white px-3 py-2 font-sans text-sm" placeholder="Пошук" [(ngModel)]="search" (keyup.enter)="load()" />
        <select class="border border-memorial-line bg-white px-3 py-2 font-sans text-sm" [(ngModel)]="status" (ngModelChange)="load()">
          <option [ngValue]="undefined">Усі статуси</option>
          <option value="Draft">Чернетка</option>
          <option value="Published">Опубліковано</option>
          <option value="Archived">В архіві</option>
        </select>
        <select class="border border-memorial-line bg-white px-3 py-2 font-sans text-sm" [(ngModel)]="demoFilter" (ngModelChange)="load()">
          <option value="all">Усі</option>
          <option value="client">Клієнтські</option>
          <option value="demo">Демо</option>
        </select>
        <button type="button" class="border border-memorial-line px-3 py-2 font-sans text-sm" (click)="load()">Фільтр</button>
      </div>

      <div class="overflow-x-auto border border-memorial-line bg-white">
        <table class="min-w-full font-sans text-sm">
          <thead class="border-b border-memorial-line text-left text-memorial-muted">
            <tr>
              <th class="px-4 py-3">Фото</th>
              <th class="px-4 py-3">ПІБ</th>
              <th class="px-4 py-3">План</th>
              <th class="px-4 py-3">Оплата</th>
              <th class="px-4 py-3">PublicId</th>
              <th class="px-4 py-3">Статус</th>
              <th class="px-4 py-3">Видимість</th>
              <th class="px-4 py-3">Оновлено</th>
              <th class="px-4 py-3">Дії</th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr class="border-b border-memorial-line hover:bg-[#FAFAF8]">
                <td class="px-4 py-3">
                  @if (item.mainPhotoThumbUrl || item.mainPhotoPreviewUrl) {
                    <img [src]="item.mainPhotoThumbUrl || item.mainPhotoPreviewUrl" alt="" class="h-12 w-12 object-cover" />
                  } @else {
                    <span class="text-memorial-muted">—</span>
                  }
                </td>
                <td class="px-4 py-3">
                  {{ item.fullName }}
                  @if (item.isDemo) {
                    <span class="ml-2 inline-block border border-memorial-line px-1.5 py-px align-middle text-[0.65rem] font-medium uppercase tracking-[0.14em] text-memorial-muted">Демо</span>
                  }
                </td>
                <td class="px-4 py-3">{{ item.planName || '—' }}</td>
                <td class="px-4 py-3 text-memorial-muted">
                  @if (item.finalPrice != null) {
                    {{ item.finalPrice | number:'1.0-0' }} грн ·
                  }
                  {{ paymentLabels[item.paymentStatus || 'Unpaid'] }}
                </td>
                <td class="px-4 py-3">{{ item.publicId }}</td>
                <td class="px-4 py-3">{{ statusLabels[item.status] }}</td>
                <td class="px-4 py-3">{{ privacyLabels[item.privacy] }}</td>
                <td class="px-4 py-3">{{ item.updatedAt | date:'short' }}</td>
                <td class="px-4 py-3">
                  <div class="flex flex-wrap gap-2">
                    <a [routerLink]="editLink(item.id)" class="underline">Редагувати</a>
                    <a [routerLink]="previewLink(item.id)" class="underline">Перегляд</a>
                    @if (item.status !== 'Archived') {
                      <button type="button" class="text-memorial-muted underline" (click)="archive(item.id)">Архівувати</button>
                    }
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      @if (showCreate) {
        <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" (click)="closeCreate()">
          <div class="max-h-[90vh] w-full max-w-lg overflow-y-auto border border-memorial-line bg-white p-6 shadow-lg" (click)="$event.stopPropagation()">
            <h2 class="font-serif text-2xl">Новий меморіал</h2>
            <p class="mt-2 font-sans text-sm text-memorial-muted">Оберіть план і вкажіть ПІБ.</p>

            <label class="mt-5 block font-sans text-sm">
              ПІБ *
              <input class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="createName" name="createName" />
            </label>

            <fieldset class="mt-5 space-y-3">
              <legend class="font-sans text-sm font-medium">Тарифний план *</legend>
              @for (plan of plans; track plan.id) {
                <label class="flex cursor-pointer items-start gap-3 border border-memorial-line p-3 hover:bg-[#FAFAF8]">
                  <input type="radio" class="mt-1" name="planId" [value]="plan.id" [(ngModel)]="createPlanId" />
                  <span class="min-w-0">
                    <span class="block font-serif text-lg">{{ plan.name }}</span>
                    <span class="mt-0.5 block font-sans text-sm text-memorial-muted">
                      {{ plan.price | number:'1.0-0' }} ₴
                      @if (plan.isCustom) {
                        · індивідуальний
                      }
                    </span>
                    @if (plan.description) {
                      <span class="mt-1 block font-sans text-xs text-memorial-muted">{{ plan.description }}</span>
                    }
                  </span>
                </label>
              }
            </fieldset>

            <label class="mt-5 flex items-start gap-2 font-sans text-sm">
              <input type="checkbox" class="mt-1" [(ngModel)]="createIsDemo" name="createIsDemo" />
              <span>
                <strong>Демонстраційна сторінка</strong>
                <span class="mt-0.5 block text-memorial-muted">Використовується для рекламних прикладів і презентації можливостей Nezabuti.</span>
              </span>
            </label>

            @if (selectedPlan?.isCustom) {
              <div class="mt-4 space-y-3 border border-memorial-line bg-[#FAFAF8] p-4">
                <p class="font-sans text-sm font-medium">Параметри Custom</p>
                <label class="flex items-center gap-2 font-sans text-sm">
                  <input type="checkbox" [(ngModel)]="customUnlimited" name="customUnlimited" />
                  Безлімітний
                </label>
                <label class="block font-sans text-sm">
                  Ціна
                  <input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="customPrice" name="customPrice" />
                </label>
                @if (!customUnlimited) {
                  <div class="grid gap-2 sm:grid-cols-2">
                    <label class="block font-sans text-xs">Блоків<input type="number" class="mt-1 w-full border px-2 py-1" [(ngModel)]="customMaxBlocks" name="cmb" /></label>
                    <label class="block font-sans text-xs">Галерей<input type="number" class="mt-1 w-full border px-2 py-1" [(ngModel)]="customMaxGalleries" name="cmg" /></label>
                    <label class="block font-sans text-xs">Фото/галерея<input type="number" class="mt-1 w-full border px-2 py-1" [(ngModel)]="customMaxPhotos" name="cmp" /></label>
                    <label class="block font-sans text-xs">Події<input type="number" class="mt-1 w-full border px-2 py-1" [(ngModel)]="customMaxTimeline" name="cmt" /></label>
                    <label class="block font-sans text-xs">Спогади<input type="number" class="mt-1 w-full border px-2 py-1" [(ngModel)]="customMaxMemories" name="cmm" /></label>
                  </div>
                }
                <label class="block font-sans text-sm">
                  Включені оновлення
                  <input type="number" class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="customIncludedUpdates" name="ciu" />
                </label>
              </div>
            }

            @if (createError) {
              <p class="mt-3 font-sans text-sm text-red-700">{{ createError }}</p>
            }

            <div class="mt-6 flex justify-end gap-2">
              <button type="button" class="border border-memorial-line px-4 py-2 font-sans text-sm" (click)="closeCreate()">Скасувати</button>
              <button
                type="button"
                class="bg-memorial-ink px-4 py-2 font-sans text-sm text-white disabled:opacity-50"
                [disabled]="creating"
                (click)="submitCreate()"
              >
                {{ creating ? 'Створення…' : 'Створити' }}
              </button>
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class AdminMemorialListPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  items: MemorialListItem[] = [];
  plans: Plan[] = [];
  search = '';
  status: MemorialStatus | undefined;
  demoFilter: 'all' | 'client' | 'demo' = 'all';
  readonly statusLabels = STATUS_LABELS;
  readonly privacyLabels = PRIVACY_LABELS;
  readonly paymentLabels = PAYMENT_STATUS_LABELS;

  showCreate = false;
  creating = false;
  createName = '';
  createPlanId = '';
  createIsDemo = false;
  createError = '';
  customUnlimited = true;
  customPrice = 0;
  customMaxBlocks: number | null = null;
  customMaxGalleries: number | null = null;
  customMaxPhotos: number | null = null;
  customMaxTimeline: number | null = null;
  customMaxMemories: number | null = null;
  customIncludedUpdates = 0;

  get selectedPlan(): Plan | undefined {
    return this.plans.find((p) => p.id === this.createPlanId);
  }

  ngOnInit(): void {
    this.load();
    this.api.listPlans().subscribe({ next: (p) => (this.plans = p.filter((x) => x.isActive)) });
  }

  editLink(id: string): string {
    return adminUrl('memorials', id);
  }

  previewLink(id: string): string {
    return adminUrl('preview', id);
  }

  load(): void {
    const isDemo = this.demoFilter === 'all' ? undefined : this.demoFilter === 'demo';
    this.api.listMemorials({ search: this.search || undefined, status: this.status, isDemo }).subscribe((r) => {
      this.items = r.items;
    });
  }

  openCreate(): void {
    this.showCreate = true;
    this.createError = '';
    this.createName = '';
    this.createIsDemo = false;
    this.createPlanId = this.plans.find((p) => p.code === 'Story')?.id || this.plans[0]?.id || '';
  }

  closeCreate(): void {
    if (this.creating) {
      return;
    }
    this.showCreate = false;
  }

  submitCreate(): void {
    if (!this.createName.trim()) {
      this.createError = 'Вкажіть ПІБ.';
      return;
    }
    if (!this.createPlanId) {
      this.createError = 'Оберіть план.';
      return;
    }

    this.creating = true;
    this.createError = '';

    const custom = this.selectedPlan?.isCustom
      ? {
          price: this.customPrice,
          isUnlimited: this.customUnlimited,
          maxBlocks: this.customUnlimited ? null : this.customMaxBlocks,
          maxGalleryBlocks: this.customUnlimited ? null : this.customMaxGalleries,
          maxPhotosPerGallery: this.customUnlimited ? null : this.customMaxPhotos,
          maxTimelineEvents: this.customUnlimited ? null : this.customMaxTimeline,
          maxMemories: this.customUnlimited ? null : this.customMaxMemories,
          includedUpdates: this.customIncludedUpdates
        }
      : null;

    this.api
      .createMemorial({
        fullName: this.createName.trim(),
        planId: this.createPlanId,
        isDemo: this.createIsDemo,
        customOverrides: custom
      })
      .subscribe({
        next: (m) => {
          this.creating = false;
          this.showCreate = false;
          void this.router.navigateByUrl(adminUrl('memorials', m.id));
        },
        error: (err) => {
          this.creating = false;
          this.createError = err?.error?.message || 'Не вдалося створити меморіал';
        }
      });
  }

  archive(id: string): void {
    if (!confirm('Архівувати меморіал?')) {
      return;
    }
    this.api.archive(id).subscribe(() => this.load());
  }
}
