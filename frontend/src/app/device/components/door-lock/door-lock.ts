import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { CardModule } from 'primeng/card';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { DoorLockState } from '../../models/device-types';

/* ─────────────── Constants ─────────────── */

const VIEWBOX = 200;
const CX = 100;

/**
 * Animated padlock with shackle pop-open. The SVG itself is the primary
 * tap target — clicking the lock toggles it. A p-toggleswitch below
 * gives a redundant standard control surface for keyboard users and
 * for visual consistency with the other device cards.
 *
 * Door Lock is a *latch* device per 1.1 of the spec — always energized,
 * no power state. The state machine operates entirely at the lock/unlock
 * level. "On" for filtering purposes always means "this device is here";
 * latch devices are excluded from "Off" filters.
 */
@Component({
  selector: 'aura-door-lock',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardModule,
    ToggleSwitchModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-card styleClass="lock-card" [style.--lock-glow]="cardGlowColor()">
      <ng-template pTemplate="header">
        <div class="lock-card-head">
          <h3 class="lock-name">{{ name() }}</h3>
          <p class="lock-loc">{{ location() }}</p>
        </div>
      </ng-template>

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
            <rect
              class="lock-body"
              [attr.x]="60"
              [attr.y]="100"
              [attr.width]="80"
              [attr.height]="76"
              rx="10"
              [attr.fill]="bodyFill()"
            />
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
      <div class="control-row lock-row">
        <p-toggleswitch
          [inputId]="lockId"
          [ngModel]="isLocked()"
          (ngModelChange)="onSwitchToggle($event)"
        />
      </div>
    </p-card>
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

  /* ─────────────── Unique IDs ─────────────── */

  private readonly _uid = Math.random().toString(36).slice(2, 9);
  readonly lockId = `lock-${this._uid}`;

  /* ─────────────── Computed visual state ─────────────── */

  readonly isLocked = computed(() => this.lockState() === DoorLockState.Locked);

  readonly stateLabel = computed(() =>
    this.isLocked() ? 'Locked' : 'Unlocked'
  );

  /** Body fill: warm accent when locked, muted when unlocked. */
  readonly bodyFill = computed(() =>
    this.isLocked() ? '#C2410C' : '#888780'
  );

  readonly keyholeFill = computed(() =>
    this.isLocked() ? '#FBF8F3' : '#3a342b'
  );

  /**
   * Card glow color
   */
  readonly cardGlowColor = computed(() =>
    this.isLocked() ? 'rgba(194, 65, 12, 0.15)' : 'transparent'
  );

  /**
   * Shackle path (the U-shape).
   */
  shacklePath(): string {
    const leftX = 76;
    const rightX = 124;
    const baseY = 100;
    const archHeight = 53;

    return `M ${leftX} ${baseY}
            L ${leftX} ${baseY - archHeight + 20}
            A 24 24 0 0 1 ${rightX} ${baseY - archHeight + 20}
            L ${rightX} ${baseY}`;
  }

  /* ─────────────── Event handlers ─────────────── */

  /** SVG click — flips current state. */
  onToggleClick(): void {
    this.emitToggled();
  }

  onSwitchToggle(_locked: boolean): void {
    this.emitToggled();
  }

  /**
   * Two paths can never disagree on what
   * "toggle" means.
   */
  private emitToggled(): void {
    const next = this.isLocked()
      ? DoorLockState.Unlocked
      : DoorLockState.Locked;
    this.lockStateChange.emit(next);
  }
}
