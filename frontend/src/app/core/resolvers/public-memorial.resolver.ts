import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { catchError, of } from 'rxjs';
import { ApiService } from '../services/api.service';
import { PublicMemorial } from '../models/memorial.models';
import { SsrResponseService } from '../services/ssr-response.service';

export const publicMemorialResolver: ResolveFn<PublicMemorial | null> = (route) => {
  const publicId = route.paramMap.get('publicId');
  const api = inject(ApiService);
  const ssr = inject(SsrResponseService);

  if (!publicId) {
    ssr.notFound();
    return of(null);
  }

  return api.getPublicMemorial(publicId).pipe(
    catchError(() => {
      ssr.notFound();
      return of(null);
    })
  );
};
