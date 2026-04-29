import { Routes } from '@angular/router';
import { authGuard } from './authentication/guard/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./authentication/component/login/login').then((m) => m.Login),
  },
  {
    path: '',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: '', redirectTo: 'devices', pathMatch: 'full' },
      {
        path: 'devices',
        loadComponent: () =>
          import('./device/components/device-list/device-list').then(
            (m) => m.DeviceList
          ),
      },
      {
        path: 'devices/:id',
        loadComponent: () =>
          import('./device/components/device-detail/device-detail').then(
            (m) => m.DeviceDetail
          ),
      },
      {
        path: 'scenes',
        loadComponent: () =>
          import('./scene/components/scene-list/scene-list').then(
            (m) => m.SceneList
          ),
      },
    ],
  },
];
