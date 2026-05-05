import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../service/auth.service';
import { of } from 'rxjs';

describe('authGuard', () => {
  let authServiceMock: any;
  let routerMock: any;

  beforeEach(() => {
    authServiceMock = {
      isAuthenticated: vi.fn()
    };
    routerMock = {
      createUrlTree: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    });
  });

  it('should allow navigation when authenticated', () => {
    authServiceMock.isAuthenticated.mockReturnValue(true);
    const result = TestBed.runInInjectionContext(() => authGuard({} as any, { url: '/test' } as any));
    expect(result).toBe(true);
  });

  it('should redirect to login when not authenticated', () => {
    authServiceMock.isAuthenticated.mockReturnValue(false);
    const urlTree = {};
    routerMock.createUrlTree.mockReturnValue(urlTree);

    const result = TestBed.runInInjectionContext(() => authGuard({} as any, { url: '/test' } as any));

    expect(result).toBe(urlTree);
    expect(routerMock.createUrlTree).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/test' } });
  });
});
