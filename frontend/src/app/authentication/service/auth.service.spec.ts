import { TestBed } from '@angular/core/testing';
import { OAuthService, OAuthEvent } from 'angular-oauth2-oidc';
import { AuthService } from './auth.service';
import { Subject } from 'rxjs';

describe('AuthService', () => {
  let service: AuthService;
  let oauthServiceMock: any;
  let eventsSubject: Subject<OAuthEvent>;

  beforeEach(() => {
    eventsSubject = new Subject<OAuthEvent>();
    oauthServiceMock = {
      configure: vi.fn(),
      events: eventsSubject.asObservable(),
      loadDiscoveryDocumentAndTryLogin: vi.fn().mockResolvedValue(undefined),
      setupAutomaticSilentRefresh: vi.fn(),
      initCodeFlow: vi.fn(),
      logOut: vi.fn(),
      getAccessToken: vi.fn().mockReturnValue('fake-token'),
      hasValidAccessToken: vi.fn().mockReturnValue(true),
      getIdentityClaims: vi.fn().mockReturnValue({ preferred_username: 'testuser' })
    };

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: OAuthService, useValue: oauthServiceMock }
      ]
    });
    service = TestBed.inject(AuthService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialize correctly', async () => {
    await service.init();
    expect(oauthServiceMock.configure).toHaveBeenCalled();
    expect(oauthServiceMock.loadDiscoveryDocumentAndTryLogin).toHaveBeenCalled();
    expect(oauthServiceMock.setupAutomaticSilentRefresh).toHaveBeenCalled();
    expect(service.isAuthenticated()).toBe(true);
    expect(service.userName()).toBe('testuser');
  });

  it('should login', () => {
    service.login();
    expect(oauthServiceMock.initCodeFlow).toHaveBeenCalled();
  });

  it('should logout', () => {
    service.logout();
    expect(oauthServiceMock.logOut).toHaveBeenCalled();
  });

  it('should return access token', () => {
    expect(service.getAccessToken()).toBe('fake-token');
  });

  it('should update state on token received', async () => {
    await service.init();
    oauthServiceMock.hasValidAccessToken.mockReturnValue(true);
    oauthServiceMock.getIdentityClaims.mockReturnValue({ name: 'New User' });

    eventsSubject.next({ type: 'token_received' } as OAuthEvent);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.userName()).toBe('New User');
  });

  it('should clear state on logout event', async () => {
    await service.init();
    eventsSubject.next({ type: 'logout' } as OAuthEvent);

    expect(service.isAuthenticated()).toBe(false);
    expect(service.userName()).toBeNull();
  });
});
