import { Routes } from '@angular/router';
import { adminAuthGuard } from './core/guards/admin-auth.guard';
import { publicMemorialResolver } from './core/resolvers/public-memorial.resolver';
import { ADMIN_BASE_PATH } from './core/config/admin-routes';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/home/home-page.component').then((m) => m.HomePageComponent)
  },
  {
    path: 'm/:publicId',
    loadComponent: () => import('./pages/memorial/memorial-page.component').then((m) => m.MemorialPageComponent),
    resolve: { memorial: publicMemorialResolver }
  },
  {
    path: `${ADMIN_BASE_PATH}/login`,
    loadComponent: () =>
      import('./pages/admin/login/admin-login-page.component').then((m) => m.AdminLoginPageComponent)
  },
  {
    path: `${ADMIN_BASE_PATH}/preview/:id`,
    canActivate: [adminAuthGuard],
    loadComponent: () =>
      import('./pages/admin/preview/admin-preview-page.component').then((m) => m.AdminPreviewPageComponent)
  },
  {
    path: ADMIN_BASE_PATH,
    canActivate: [adminAuthGuard],
    loadComponent: () =>
      import('./pages/admin/layout/admin-layout.component').then((m) => m.AdminLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/admin/dashboard/admin-dashboard-page.component').then(
            (m) => m.AdminDashboardPageComponent
          )
      },
      {
        path: 'memorials',
        loadComponent: () =>
          import('./pages/admin/memorial-list/admin-memorial-list-page.component').then(
            (m) => m.AdminMemorialListPageComponent
          )
      },
      {
        path: 'memorials/:id',
        loadComponent: () =>
          import('./pages/admin/memorial-editor/admin-memorial-editor-page.component').then(
            (m) => m.AdminMemorialEditorPageComponent
          )
      },
      {
        path: 'archive',
        loadComponent: () =>
          import('./pages/admin/archive/admin-archive-page.component').then(
            (m) => m.AdminArchivePageComponent
          )
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./pages/admin/settings/admin-settings-page.component').then(
            (m) => m.AdminSettingsPageComponent
          )
      }
    ]
  },
  // Old /admin paths intentionally fall through to 404 — no redirect that reveals the new URL
  {
    path: '**',
    loadComponent: () =>
      import('./pages/not-found/not-found-page.component').then((m) => m.NotFoundPageComponent)
  }
];
