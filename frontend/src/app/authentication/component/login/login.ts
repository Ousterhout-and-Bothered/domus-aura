import { Component, effect, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../service/auth.service';

/**
 * Landing page for unauthenticated users. Presents a "Sign in" button that
 * redirects to Keycloak's hosted login page (Authorization Code + PKCE).
 *
 * If the user is already authenticated when they land here (e.g. they
 * bookmarked /login), they're forwarded to /devices or to the originally
 * requested route via the `returnUrl` query param set by the auth guard.
 */
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ButtonModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isAuthenticated = this.auth.isAuthenticated;

  constructor() {
    // If auth state flips to true while on this page, leave immediately.
    effect(() => {
      if (this.isAuthenticated()) {
        const target =
          this.route.snapshot.queryParamMap.get('returnUrl') ?? '/devices';
        this.router.navigateByUrl(target);
      }
    });
  }

  signIn(): void {
    this.auth.login();
  }
}
