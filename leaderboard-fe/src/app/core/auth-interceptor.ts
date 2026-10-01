import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { from, switchMap } from 'rxjs';
import { ClerkService } from 'ngx-clerk';

// Usamos la versión funcional del interceptor disponible en Angular standalone API
export const clerkAuthInterceptor: HttpInterceptorFn = (req, next) => {
  const clerk = inject(ClerkService);
  return from(clerk.getToken()).pipe(
    switchMap((token) => next(token
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req))
  );
};
