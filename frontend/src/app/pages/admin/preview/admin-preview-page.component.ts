import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { ApiService } from '../../../core/services/api.service';
import { PublicMemorial } from '../../../core/models/memorial.models';
import { MemorialViewComponent } from '../../../shared/components/memorial-view/memorial-view.component';
import { adminUrl } from '../../../core/config/admin-routes';

@Component({
  selector: 'app-admin-preview-page',
  standalone: true,
  imports: [RouterLink, MemorialViewComponent],
  template: `
    <div class="min-h-screen bg-memorial-bg">
      <div class="border-b border-memorial-line bg-white px-6 py-3">
        <div class="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3">
          <div class="flex flex-wrap items-center gap-3">
            <p class="font-sans text-sm text-memorial-muted">Режим перегляду — статистика не змінюється</p>
            @if (memorial?.isDemo) {
              <span class="border border-memorial-line px-2 py-0.5 font-sans text-xs text-memorial-ink">Демонстраційна сторінка</span>
            }
          </div>
          <div class="flex gap-3 font-sans text-sm">
            @if (editHref) {
              <a [routerLink]="editHref" class="underline">До редагування</a>
            }
            <a [routerLink]="listHref" class="underline">Список</a>
          </div>
        </div>
      </div>

      @if (loading) {
        <div class="mx-auto max-w-3xl animate-pulse space-y-6 px-6 py-16" aria-busy="true" aria-label="Завантаження">
          <div class="mx-auto h-64 max-w-sm bg-memorial-line/60"></div>
          <div class="mx-auto h-8 max-w-md bg-memorial-line/60"></div>
          <div class="mx-auto h-4 max-w-xs bg-memorial-line/40"></div>
        </div>
      } @else if (error) {
        <div class="flex min-h-[50vh] items-center justify-center font-sans text-red-700">{{ error }}</div>
      } @else if (memorial) {
        <app-memorial-view [memorial]="memorial" />
      }
    </div>
  `
})
export class AdminPreviewPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  memorial: PublicMemorial | null = null;
  editHref: string | null = null;
  readonly listHref = adminUrl('memorials');
  loading = true;
  error = '';

  ngOnInit(): void {
    this.meta.updateTag({ name: 'robots', content: 'noindex,nofollow' });
    const memorialId = this.route.snapshot.paramMap.get('id');
    if (!memorialId) {
      this.loading = false;
      this.error = 'Невірний ідентифікатор';
      return;
    }

    this.editHref = adminUrl('memorials', memorialId);

    this.api.getAdminPreview(memorialId).subscribe({
      next: (m) => {
        this.memorial = m;
        this.loading = false;
        this.title.setTitle(`${m.fullName} (перегляд) — Nezabuti`);
        this.meta.updateTag({ name: 'robots', content: 'noindex,nofollow' });
      },
      error: () => {
        this.loading = false;
        this.error = 'Не вдалося завантажити перегляд';
      }
    });
  }
}
