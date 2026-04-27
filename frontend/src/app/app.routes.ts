import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'devices', pathMatch: 'full' },
  { path: 'devices', loadComponent: () => import('./device/components/device-list/device-list').then(m => m.DeviceList) },
  { path: 'devices/:id', loadComponent: () => import('./device/components/device-detail/device-detail').then(m => m.DeviceDetail) },
  { path: 'simulation', loadComponent: () => import('./simulation/components/simulation-controls/simulation-controls').then(m => m.SimulationControls) },
  { path: 'scenes', loadComponent: () => import('./scene/components/scene-list/scene-list').then(m => m.SceneList) },
];
