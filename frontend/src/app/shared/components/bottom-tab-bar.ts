import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

/**
 * Mobile-only bottom tab bar. Mirrors the primary nav (Devices, Scenes)
 * that lives in the header on tablet+ viewports. Hidden via display:none
 * at the md breakpoint and up — header nav takes over there.
 *
 * Fixed position, full width, sits above safe-area inset. Page content
 * clears it via padding-bottom on .aura-main.
 */
@Component({
  selector: 'aura-bottom-tab-bar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="bottom-tab-bar" aria-label="Primary">

      <a routerLink="/devices"
         routerLinkActive="is-active"
         class="bottom-tab"
      >
        <i class="pi pi-th-large bottom-tab-icon" aria-hidden="true"></i>
        <span class="bottom-tab-label">Devices</span>
      </a>
      <a routerLink="/scenes"
         routerLinkActive="is-active"
         class="bottom-tab"
      >
        <i class="pi pi-play bottom-tab-icon" aria-hidden="true"></i>
        <span class="bottom-tab-label">Scenes</span>
      </a>
    </nav>
  `,
  styleUrl: './bottom-tab-bar.scss',
})
export class BottomTabBar {}
