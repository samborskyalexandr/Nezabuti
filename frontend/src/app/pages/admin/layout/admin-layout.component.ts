import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { AuthService } from '../../../core/services/auth.service';
import { adminUrl } from '../../../core/config/admin-routes';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-screen bg-[#F4F2EE] text-memorial-ink">
      <header class="border-b border-memorial-line bg-white px-6 py-4">
        <div class="mx-auto flex max-w-6xl items-center justify-between gap-4">
          <div class="flex items-center gap-6">
            <a [routerLink]="root" class="font-serif text-xl">Nezabuti</a>
            <nav class="hidden gap-4 font-sans text-sm md:flex">
              <a [routerLink]="root" routerLinkActive="font-semibold" [routerLinkActiveOptions]="{ exact: true }">Панель</a>
              <a [routerLink]="memorials" routerLinkActive="font-semibold">Меморіали</a>
              <a [routerLink]="archive" routerLinkActive="font-semibold">Архів</a>
              <a [routerLink]="settings" routerLinkActive="font-semibold">Налаштування</a>
            </nav>
          </div>
          <button type="button" class="font-sans text-sm text-memorial-muted" (click)="logout()">Вийти</button>
        </div>
      </header>
      <main class="mx-auto max-w-6xl px-6 py-8">
        <router-outlet />
      </main>
    </div>
  `
})
export class AdminLayoutComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly meta = inject(Meta);
  private readonly title = inject(Title);

  readonly root = adminUrl();
  readonly memorials = adminUrl('memorials');
  readonly archive = adminUrl('archive');
  readonly settings = adminUrl('settings');

  ngOnInit(): void {
    this.meta.updateTag({ name: 'robots', content: 'noindex,nofollow' });
    this.title.setTitle('Nezabuti — керування');
  }

  logout(): void {
    this.auth.clear();
    void this.router.navigateByUrl(adminUrl('login'));
  }
}
