import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { DoorLockState } from '../../models/device-types';

/* ─────────────── Constants ─────────────── */

const VIEWBOX = 200;
const CX = 100;

@Component({
  selector: 'aura-door-lock',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="lock-card">
      <header>
        <h3 class="lock-name">{{ name() }}</h3>
        <p class="lock-loc">{{ location() }}</p>
      </header>

      <div class="lock-stage">
        <button
          type="button"
          class="lock-svg-button"
          [attr.aria-label]="(isLocked() ? 'Unlock' : 'Lock') + ' ' + name()"
          (click)="onToggleClick()"
        >
          <svg
            class="lock-svg"
            [attr.viewBox]="'0 0 ' + viewBox + ' ' + viewBox"
            role="img"
          >
            <!-- Shackle (the U-shape) — rotates open when unlocked -->
            <g
              class="shackle"
              [class.shackle-open]="!isLocked()"
            >
              <path
                [attr.d]="shacklePath()"
                fill="none"
                [attr.stroke]="bodyFill()"
                stroke-width="14"
                stroke-linecap="round"
              />
            </g>

            <!-- Lock body -->
            <rect
              class="lock-body"
              [attr.x]="60"
              [attr.y]="100"
              [attr.width]="80"
              [attr.height]="76"
              rx="10"
              [attr.fill]="bodyFill()"
            />

            <!-- Keyhole -->
            <g class="keyhole" [attr.opacity]="isLocked() ? 0.85 : 0.55">
              <circle
                [attr.cx]="CX"
                [attr.cy]="130"
                r="7"
                [attr.fill]="keyholeFill()"
              />
              <rect
                [attr.x]="CX - 2.5"
                [attr.y]="130"
                width="5"
                height="14"
                [attr.fill]="keyholeFill()"
              />
            </g>
          </svg>
        </button>

        <div class="lock-readout">
          <div class="lock-state-label" [class.unlocked-label]="!isLocked()">
            {{ stateLabel() }}
          </div>
        </div>
      </div>

      <!-- Toggle button -->
      <!-- Toggle button -->
      <!-- Toggle switch -->
      <div class="controls-row">
        <button
          type="button"
          class="lock-toggle-switch"
          [class.locked]="isLocked()"
          [attr.aria-label]="(isLocked() ? 'Unlock' : 'Lock') + ' ' + name()"
          [attr.aria-pressed]="isLocked()"
          role="switch"
          (click)="onToggleClick()"
        >
    <span class="toggle-track">
      <span class="toggle-thumb"></span>
    </span>
          <span class="toggle-label">
    </span>
        </button>
      </div>
    </article>
  `,
  styleUrl: './door-lock.scss',
})
export class DoorLock {
  /* ─────────────── Inputs / outputs ─────────────── */

  readonly name = input.required<string>();
  readonly location = input.required<string>();
  readonly lockState = input.required<DoorLockState>();

  readonly lockStateChange = output<DoorLockState>();

  /* ─────────────── Template constants ─────────────── */

  readonly viewBox = VIEWBOX;
  readonly CX = CX;

  /* ─────────────── Computed visual state ─────────────── */

  readonly isLocked = computed(() => this.lockState() === 'Locked');

  readonly stateLabel = computed(() => {
    return this.isLocked() ? 'Locked' : 'Unlocked';
  });

  /** Body fill: warm accent when locked, muted when unlocked. */
  readonly bodyFill = computed(() => {
    return this.isLocked() ? '#C2410C' : '#888780';
  });

  readonly keyholeFill = computed(() => {
    return this.isLocked() ? '#FBF8F3' : '#3a342b';
  });

  /**
   * Shackle path (the U-shape). It's drawn from the upper-left corner of the
   * body, up and over, then down to the upper-right corner of the body.
   * The .shackle-open class rotates the whole thing for the unlock animation.
   */
  shacklePath(): string {
    // Two anchor points where the shackle meets the body
    const leftX = 76;
    const rightX = 124;
    const baseY = 100;

    // Top of the arch
    const archHeight = 53;

    return `M ${leftX} ${baseY}
            L ${leftX} ${baseY - archHeight + 20}
            A 24 24 0 0 1 ${rightX} ${baseY - archHeight + 20}
            L ${rightX} ${baseY}`;
  }

  /* ─────────────── Event handlers ─────────────── */

  onToggleClick(): void {
    const next: DoorLockState = this.isLocked()
      ? ('Unlocked' as DoorLockState)
      : ('Locked' as DoorLockState);
    this.lockStateChange.emit(next);
  }
}
