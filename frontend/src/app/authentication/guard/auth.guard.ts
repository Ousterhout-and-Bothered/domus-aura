import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../service/auth.service';

/**
 * Route guard. Allows navigation only if the user has a valid access token.
 *
 * On unauthenticated access, the user is sent to /login (where they can
 * trigger the Keycloak redirect manually) rather than starting the OIDC
 * flow directly here. This avoids redirect loops if the IDP is unreachable
 * and gives the user a clear "Sign in" affordance.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};
