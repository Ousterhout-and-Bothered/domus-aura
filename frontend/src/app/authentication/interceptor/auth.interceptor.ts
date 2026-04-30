import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../service/auth.service';
import { environment } from '../../../environments/environment';

/**
 * Attaches `Authorization: Bearer <token>` to requests targeting the API,
 * and triggers re-authentication if the API replies 401.
 *
 * Only API requests are decorated — the OIDC discovery and token endpoints
 * must NOT carry an Authorization header (Keycloak rejects them).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const isApiRequest = req.url.startsWith(environment.apiUrl);

  const token = auth.getAccessToken();
  const decorated =
    isApiRequest && token
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(decorated).pipe(
    catchError((err) => {
      if (
        isApiRequest &&
        err instanceof HttpErrorResponse &&
        err.status === 401
      ) {
        // Token expired or revoked between silent refreshes. Send the user
        // back through Keycloak to re-acquire one.
        auth.login();
      }
      return throwError(() => err);
    })
  );
};
