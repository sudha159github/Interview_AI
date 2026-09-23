import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Adds the JWT to every request going to our API,
 * and logs the user out if the API rejects the token (401).
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken;

  // Only attach the token to OUR API, never to other websites
  const isApiRequest = request.url.startsWith('/api/');

  const outgoing =
    token && isApiRequest
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;

  return next(outgoing).pipe(
    catchError((error: unknown) => {
      // We sent a token, but the API says it's not valid (expired or revoked) → log out
      if (error instanceof HttpErrorResponse && error.status === 401 && token) {
        auth.logout();
      }

      return throwError(() => error);
    }),
  );
};