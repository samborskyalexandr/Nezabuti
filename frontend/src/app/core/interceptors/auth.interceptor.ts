import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { adminUrl } from '../config/admin-routes';

/** Attaches JWT for admin API calls; on 401 clears session and sends to login. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const isAdminApi = req.url.includes('/api/admin/');
  if (!isAdminApi) {
    return next(req);
  }

  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.getToken();
  const authedReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authedReq).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401) {
        auth.clear();
        void router.navigateByUrl(adminUrl('login'));
      }
      return throwError(() => err);
    })
  );
};
