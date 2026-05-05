import { Component, effect, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../service/auth.service';

/**
 * Component for the login page, serving as the entry point for unauthenticated users.
 *
 * It provides a sign-in button that initiates the OIDC authentication flow.
 * If the user is already authenticated, they are automatically redirected to
 * their intended destination or the default dashboard.
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

  /**
   * Triggers the OIDC login process.
   */
  signIn(): void {
    this.auth.login();
  }
}
