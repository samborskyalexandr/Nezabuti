import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { MemorialListItem } from '../../../core/models/memorial.models';
import { adminUrl } from '../../../core/config/admin-routes';

@Component({
  selector: 'app-admin-archive-page',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="space-y-6">
      <h1 class="font-serif text-3xl">Архів</h1>
      <p class="font-sans text-sm text-memorial-muted">
        Архівні меморіали можна відновити або остаточно видалити разом із усіма фото (thumb/preview/full).
      </p>
      <ul class="space-y-3">
        @for (item of items; track item.id) {
          <li class="flex flex-wrap items-center justify-between gap-3 border border-memorial-line bg-white px-4 py-3">
            <div class="flex items-center gap-3">
              @if (item.mainPhotoThumbUrl || item.mainPhotoPreviewUrl) {
                <img [src]="item.mainPhotoThumbUrl || item.mainPhotoPreviewUrl" alt="" class="h-12 w-12 object-cover" />
              }
              <div>
                <a [routerLink]="editLink(item.id)" class="font-sans underline">{{ item.fullName }}</a>
                @if (item.isDemo) {
                  <span class="ml-2 inline-block border border-memorial-line px-1.5 py-px align-middle text-[0.65rem] font-medium uppercase tracking-[0.14em] text-memorial-muted">Демо</span>
                }
                <p class="font-sans text-xs text-memorial-muted">{{ item.publicId }}</p>
              </div>
            </div>
            <div class="flex gap-2">
              <button type="button" class="border border-memorial-line px-3 py-1 font-sans text-sm" (click)="restore(item.id)">Відновити</button>
              <button type="button" class="border border-red-700 px-3 py-1 font-sans text-sm text-red-700" (click)="remove(item.id)">Видалити назавжди</button>
            </div>
          </li>
        } @empty {
          <li class="font-sans text-memorial-muted">Архів порожній</li>
        }
      </ul>
    </div>
  `
})
export class AdminArchivePageComponent implements OnInit {
  private readonly api = inject(ApiService);
  items: MemorialListItem[] = [];

  ngOnInit(): void {
    this.load();
  }

  editLink(id: string): string {
    return adminUrl('memorials', id);
  }

  load(): void {
    this.api.listMemorials({ status: 'Archived' }).subscribe((r) => (this.items = r.items));
  }

  restore(id: string): void {
    this.api.restore(id).subscribe(() => this.load());
  }

  remove(id: string): void {
    if (
      !confirm(
        'Остаточно видалити меморіал? Буде видалено запис, статистику та ВЕСЬ media-контент (усі image variants). Цю дію не скасувати.'
      )
    ) {
      return;
    }
    this.api.permanentDelete(id).subscribe(() => this.load());
  }
}
