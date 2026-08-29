import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { adminUrl } from '../../../core/config/admin-routes';

@Component({
  selector: 'app-admin-dashboard-page',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="space-y-8">
      <h1 class="font-serif text-3xl">Панель</h1>
      <div class="grid gap-4 sm:grid-cols-3">
        <div class="border border-memorial-line bg-white p-5">
          <p class="font-sans text-sm text-memorial-muted">Усього</p>
          <p class="mt-2 font-serif text-3xl">{{ total }}</p>
        </div>
        <div class="border border-memorial-line bg-white p-5">
          <p class="font-sans text-sm text-memorial-muted">Опубліковано</p>
          <p class="mt-2 font-serif text-3xl">{{ published }}</p>
        </div>
        <div class="border border-memorial-line bg-white p-5">
          <p class="font-sans text-sm text-memorial-muted">В архіві</p>
          <p class="mt-2 font-serif text-3xl">{{ archived }}</p>
        </div>
      </div>
      <a [routerLink]="memorialsLink" class="inline-block border border-memorial-ink px-4 py-2 font-sans text-sm">До списку меморіалів</a>
    </div>
  `
})
export class AdminDashboardPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  total = 0;
  published = 0;
  archived = 0;
  readonly memorialsLink = adminUrl('memorials');

  ngOnInit(): void {
    const ignore = { error: () => undefined };
    this.api.listMemorials({ pageSize: 1 }).subscribe({ next: (r) => (this.total = r.total), ...ignore });
    this.api
      .listMemorials({ status: 'Published', pageSize: 1 })
      .subscribe({ next: (r) => (this.published = r.total), ...ignore });
    this.api
      .listMemorials({ status: 'Archived', pageSize: 1 })
      .subscribe({ next: (r) => (this.archived = r.total), ...ignore });
  }
}
