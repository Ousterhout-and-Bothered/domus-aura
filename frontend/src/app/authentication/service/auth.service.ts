import { Injectable, computed, inject, signal } from '@angular/core';
import { OAuthService, AuthConfig, OAuthEvent } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';


export const authConfig: AuthConfig = {
  issuer: environment.oidc.issuer,
  clientId: environment.oidc.clientId,
  redirectUri: window.location.origin + '/',
  postLogoutRedirectUri: window.location.origin + '/login',
  responseType: 'code',
  scope: 'openid profile email',
  showDebugInformation: !environment.production,
  requireHttps: false,
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oauth = inject(OAuthService);

  private readonly _isAuthenticated = signal(false);
  private readonly _claims = signal<Record<string, unknown> | null>(null);

  /** True once a valid access token is held. */
  readonly isAuthenticated = this._isAuthenticated.asReadonly();

  /** Display name for the header. Falls back through preferred_username, name, email. */
  readonly userName = computed(() => {
    const c = this._claims();
    if (!c) return null;
    return (
      (c['preferred_username'] as string | undefined) ??
      (c['name'] as string | undefined) ??
      (c['email'] as string | undefined) ??
      null
    );
  });

  /**
   * Bootstraps OIDC. Called once from APP_INITIALIZER. Resolves once the
   * discovery document is loaded and any pending redirect callback is handled.
   */
  async init(): Promise<void> {
    this.oauth.configure(authConfig);

    // Update local state on every relevant lifecycle event.
    this.oauth.events.subscribe((event: OAuthEvent) => {
      if (
        event.type === 'token_received' ||
        event.type === 'token_refreshed' ||
        event.type === 'silently_refreshed'
      ) {
        this.refreshState();
      } else if (event.type === 'logout' || event.type === 'session_terminated') {
        this._isAuthenticated.set(false);
        this._claims.set(null);
      }
    });

    await this.oauth.loadDiscoveryDocumentAndTryLogin();
    this.oauth.setupAutomaticSilentRefresh();
    this.refreshState();
  }

  /** Kicks off the OIDC redirect to Keycloak's login page. */
  login(): void {
    this.oauth.initCodeFlow();
  }

  /** Clears local tokens and redirects to Keycloak's end-session endpoint. */
  logout(): void {
    // logOut() handles both local cleanup and the IDP redirect when the
    // discovery document advertises end_session_endpoint.
    this.oauth.logOut();
  }

  /** Current access token, if any. Used by the HTTP interceptor. */
  getAccessToken(): string | null {
    return this.oauth.getAccessToken() || null;
  }

  private refreshState(): void {
    const valid = this.oauth.hasValidAccessToken();
    this._isAuthenticated.set(valid);
    this._claims.set(valid ? (this.oauth.getIdentityClaims() ?? null) : null);
  }
}
