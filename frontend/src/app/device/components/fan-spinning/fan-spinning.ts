import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FanSpeed } from '../../models/device-types';
import { PowerState } from '../../models/device';

/* ─────────────── Geometry constants ─────────────── */

const VIEWBOX = 200;
const CX = 100;
const CY = 100;
const BACKDROP_R = 90;
const HUB_R = 12;
const BLADE_COUNT = 3;

@Component({
  selector: 'aura-fan-spinning',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="fan-card">
      <header>
        <h3 class="fan-name">{{ name() }}</h3>
        <p class="fan-loc">{{ location() }}</p>
      </header>

      <div class="fan-stage">
        <svg
          class="fan-svg"
          [attr.viewBox]="'0 0 ' + viewBox + ' ' + viewBox"
          [attr.aria-label]="'Fan. Power ' + powerState() + ', speed ' + speed()"
          role="img"
        >
          <!-- Light backdrop -->
          <circle
            [attr.cx]="CX"
            [attr.cy]="CY"
            [attr.r]="BACKDROP_R"
            fill="#FBF8F3"
          />

          <!-- Outer grille ring (decorative) -->
          <circle
            [attr.cx]="CX"
            [attr.cy]="CY"
            [attr.r]="BACKDROP_R - 4"
            fill="none"
            stroke="#3a342b"
            stroke-width="1.5"
            opacity="0.3"
          />

          <!-- Spinning blade group -->
          <g
            class="fan-blades"
            [class.spinning]="isOn()"
            [class.speed-low]="speedClass() === 'low'"
            [class.speed-medium]="speedClass() === 'medium'"
            [class.speed-high]="speedClass() === 'high'"
            [class.fan-off]="!isOn()"
          >
            @for (blade of blades; track blade) {
              <path
                [attr.d]="bladePath()"
                [attr.transform]="'rotate(' + (blade * 120) + ' ' + CX + ' ' + CY + ')'"
                [attr.fill]="bladeFill()"
                opacity="0.85"
              />
            }
          </g>

          <!-- Center hub (does not spin) -->
          <circle
            [attr.cx]="CX"
            [attr.cy]="CY"
            [attr.r]="HUB_R"
            fill="#3a342b"
          />
          <circle
            [attr.cx]="CX"
            [attr.cy]="CY"
            [attr.r]="HUB_R - 5"
            fill="#FBF8F3"
            opacity="0.3"
          />
        </svg>

        <div class="fan-readout">
          <div class="fan-state-label">{{ stateLabel() }}</div>
        </div>
      </div>

      <div class="controls-row">
        <button
          type="button"
          class="power-btn"
          [class.on]="isOn()"
          (click)="onPowerClick()"
        >
          {{ isOn() ? 'On' : 'Off' }}
        </button>
      </div>

      <div class="speed-row" [class.disabled]="!isOn()">
        <label>Speed</label>
        <div class="speed-buttons">
          @for (s of SPEEDS; track s) {
            <button
              type="button"
              class="speed-btn"
              [class.active]="speed() === s"
              [disabled]="!isOn()"
              (click)="onSpeedClick(s)"
            >
              {{ s }}
            </button>
          }
        </div>
      </div>
    </article>
  `,
  styleUrl: './fan-spinning.scss',
})
export class FanSpinning {
  /* ─────────────── Inputs / outputs ─────────────── */

  readonly name = input.required<string>();
  readonly location = input.required<string>();
  readonly powerState = input.required<PowerState>();
  readonly speed = input.required<FanSpeed>();

  readonly powerStateChange = output<PowerState>();
  readonly speedChange = output<FanSpeed>();

  /* ─────────────── Template constants ─────────────── */

  readonly viewBox = VIEWBOX;
  readonly CX = CX;
  readonly CY = CY;
  readonly BACKDROP_R = BACKDROP_R;
  readonly HUB_R = HUB_R;
  readonly blades = Array.from({ length: BLADE_COUNT }, (_, i) => i);
  readonly SPEEDS: FanSpeed[] = [
    'Low' as FanSpeed,
    'Medium' as FanSpeed,
    'High' as FanSpeed,
  ];

  /* ─────────────── Computed visual state ─────────────── */

  readonly isOn = computed(() => this.powerState() === 'On');

  readonly speedClass = computed(() => {
    if (!this.isOn()) return 'off';
    return this.speed().toLowerCase();
  });

  readonly stateLabel = computed(() => {
    if (!this.isOn()) return 'Off';
    return this.speed();
  });

  readonly bladeFill = computed(() => {
    return this.isOn() ? 'var(--aura-accent)' : '#888780';
  });

  /* ─────────────── Blade shape ─────────────── */

  /**
   * Builds an asymmetric, gently curved blade. The blade attaches at the hub
   * and extends to ~80% of the backdrop radius. Asymmetry implies direction
   * of rotation when stationary.
   */
  bladePath(): string {
    const tipR = BACKDROP_R - 18;     // how far tip extends from center
    const baseHalfWidth = 8;           // half-width at hub
    const tipHalfWidth = 14;           // half-width at tip (wider tip = scoop shape)
    const offset = 6;                  // asymmetric offset gives a subtle blade twist

    return `M ${CX - baseHalfWidth} ${CY}
            Q ${CX - baseHalfWidth - offset} ${CY - tipR / 2} ${CX - tipHalfWidth} ${CY - tipR}
            L ${CX + tipHalfWidth} ${CY - tipR}
            Q ${CX + baseHalfWidth + offset} ${CY - tipR / 2} ${CX + baseHalfWidth} ${CY}
            Z`;
  }

  /* ─────────────── Event handlers ─────────────── */

  onPowerClick(): void {
    const next = this.isOn() ? 'Off' : 'On';
    this.powerStateChange.emit(next as PowerState);
  }

  onSpeedClick(speed: FanSpeed): void {
    if (!this.isOn()) return;
    if (speed !== this.speed()) {
      this.speedChange.emit(speed);
    }
  }
}
