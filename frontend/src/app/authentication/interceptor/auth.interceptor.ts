import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../service/auth.service';
import { environment } from '../../../environments/environment';

/**
 * Interceptor that attaches the OIDC access token to outgoing API requests.
 *
 * @param req - The outgoing HTTP request.
 * @param next - The next interceptor or backend handler.
 * @returns An observable of the HTTP event stream.
 *
 * It adds an `Authorization: Bearer <token>` header to requests targeting the API.
 * If the API responds with a 401 Unauthorized error, it triggers the authentication flow.
 * Note: Discovery and token endpoints are excluded to prevent Keycloak rejection.
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
