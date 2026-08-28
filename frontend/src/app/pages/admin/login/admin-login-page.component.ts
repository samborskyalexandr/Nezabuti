import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { adminUrl } from '../../../core/config/admin-routes';

@Component({
  selector: 'app-admin-login-page',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="flex min-h-screen items-center justify-center bg-[#F4F2EE] px-6">
      <form class="w-full max-w-sm space-y-4 border border-memorial-line bg-white p-8" (ngSubmit)="submit()">
        <h1 class="font-serif text-2xl">Вхід · Nezabuti</h1>
        <label class="block font-sans text-sm">
          Логін
          <input class="mt-1 w-full border border-memorial-line px-3 py-2" [(ngModel)]="username" name="username" required />
        </label>
        <label class="block font-sans text-sm">
          Пароль
          <input class="mt-1 w-full border border-memorial-line px-3 py-2" type="password" [(ngModel)]="password" name="password" required />
        </label>
        @if (error) {
          <p class="font-sans text-sm text-red-700">{{ error }}</p>
        }
        <button type="submit" class="w-full bg-memorial-ink px-4 py-2 font-sans text-sm text-white" [disabled]="loading">
          {{ loading ? 'Вхід…' : 'Увійти' }}
        </button>
      </form>
    </div>
  `
})
export class AdminLoginPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly meta = inject(Meta);
  private readonly title = inject(Title);

  username = '';
  password = '';
  loading = false;
  error = '';

  ngOnInit(): void {
    this.meta.updateTag({ name: 'robots', content: 'noindex,nofollow' });
    this.title.setTitle('Вхід — Nezabuti');
  }

  submit(): void {
    this.loading = true;
    this.error = '';
    this.api.login(this.username, this.password).subscribe({
      next: (res) => {
        this.auth.setToken(res.token);
        this.loading = false;
        void this.router.navigateByUrl(adminUrl());
      },
      error: () => {
        this.loading = false;
        this.error = 'Невірні облікові дані';
      }
    });
  }
}
