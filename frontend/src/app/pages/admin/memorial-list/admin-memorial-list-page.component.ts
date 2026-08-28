import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import {
  MemorialListItem,
  MemorialStatus,
  PRIVACY_LABELS,
  STATUS_LABELS
} from '../../../core/models/memorial.models';
import { adminUrl } from '../../../core/config/admin-routes';

@Component({
  selector: 'app-admin-memorial-list-page',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  template: `
    <div class="space-y-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <h1 class="font-serif text-3xl">Меморіали</h1>
        <button type="button" class="bg-memorial-ink px-4 py-2 font-sans text-sm text-white" (click)="create()">Створити</button>
      </div>

      <div class="flex flex-wrap gap-3">
        <input class="border border-memorial-line bg-white px-3 py-2 font-sans text-sm" placeholder="Пошук" [(ngModel)]="search" (keyup.enter)="load()" />
        <select class="border border-memorial-line bg-white px-3 py-2 font-sans text-sm" [(ngModel)]="status" (ngModelChange)="load()">
          <option [ngValue]="undefined">Усі статуси</option>
          <option value="Draft">Чернетка</option>
          <option value="Published">Опубліковано</option>
          <option value="Archived">В архіві</option>
        </select>
        <button type="button" class="border border-memorial-line px-3 py-2 font-sans text-sm" (click)="load()">Фільтр</button>
      </div>

      <div class="overflow-x-auto border border-memorial-line bg-white">
        <table class="min-w-full font-sans text-sm">
          <thead class="border-b border-memorial-line text-left text-memorial-muted">
            <tr>
              <th class="px-4 py-3">Фото</th>
              <th class="px-4 py-3">ПІБ</th>
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
                <td class="px-4 py-3">{{ item.fullName }}</td>
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
    </div>
  `
})
export class AdminMemorialListPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  items: MemorialListItem[] = [];
  search = '';
  status: MemorialStatus | undefined;
  readonly statusLabels = STATUS_LABELS;
  readonly privacyLabels = PRIVACY_LABELS;

  ngOnInit(): void {
    this.load();
  }

  editLink(id: string): string {
    return adminUrl('memorials', id);
  }

  previewLink(id: string): string {
    return adminUrl('preview', id);
  }

  load(): void {
    this.api.listMemorials({ search: this.search || undefined, status: this.status }).subscribe((r) => {
      this.items = r.items;
    });
  }

  create(): void {
    const name = prompt('ПІБ для нової чернетки?');
    if (!name?.trim()) {
      return;
    }
    this.api.createMemorial(name.trim()).subscribe((m) => {
      void this.router.navigateByUrl(adminUrl('memorials', m.id));
    });
  }

  archive(id: string): void {
    if (!confirm('Архівувати меморіал?')) {
      return;
    }
    this.api.archive(id).subscribe(() => this.load());
  }
}
