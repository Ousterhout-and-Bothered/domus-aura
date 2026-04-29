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
import { SelectButtonModule } from 'primeng/selectbutton';

import { FanSpeed } from '../../models/device-types';
import { PowerState } from '../../models/device';

/* ─────────────── Geometry constants ─────────────── */

const VIEWBOX = 200;
const CX = 100;
const CY = 100;
const BACKDROP_R = 90;
const HUB_R = 12;
const BLADE_COUNT = 3;

/**
 * Animated three-blade ceiling fan. The blades spin via CSS animation
 * whose duration shortens with speed; at High the blades blur slightly
 * to evoke motion.
 *
 * Power is a ToggleSwitch; speed is a SelectButton (Low/Medium/High) —
 * the textbook three-option-pick-one use case for SelectButton. Speed
 * controls disable when powered off; the toggle stays available so
 * the user can power back on.
 */
@Component({
  selector: 'aura-fan-spinning',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardModule,
    ToggleSwitchModule,
    SelectButtonModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-card styleClass="fan-card">
      <ng-template pTemplate="header">
        <div class="fan-card-head">
          <h3 class="fan-name">{{ name() }}</h3>
          <p class="fan-loc">{{ location() }}</p>
        </div>
      </ng-template>

      <div class="fan-stage">
        <svg
          class="fan-svg"
          [attr.viewBox]="'0 0 ' + viewBox + ' ' + viewBox"
          [attr.aria-label]="'Fan. Power ' + powerState() + ', speed ' + speed()"
          role="img"
        >
          <circle
            [attr.cx]="CX"
            [attr.cy]="CY"
            [attr.r]="BACKDROP_R"
            fill="#FBF8F3"
          />
          <circle
            [attr.cx]="CX"
            [attr.cy]="CY"
            [attr.r]="BACKDROP_R - 4"
            fill="none"
            stroke="#3a342b"
            stroke-width="1.5"
            opacity="0.3"
          />
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
      <div class="control-row power-row">
        <label [attr.for]="powerId" class="control-label">Power</label>
        <p-toggleswitch
          [inputId]="powerId"
          [ngModel]="isOn()"
          (ngModelChange)="onPowerToggle($event)"
        />
        <span class="control-value">{{ isOn() ? 'On' : 'Off' }}</span>
      </div>
      <div class="control-row speed-row" [class.is-disabled]="!isOn()">
        <label class="control-label">Speed</label>
        <p-selectbutton
          [options]="speedOptions"
          [ngModel]="speed()"
          (onChange)="onSpeedChange($event.value)"
          [disabled]="!isOn()"
          [allowEmpty]="false"
          optionLabel="label"
          optionValue="value"
          styleClass="speed-select"></p-selectbutton>
      </div>
    </p-card>
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


  readonly speedOptions = [
    { label: 'Low', value: FanSpeed.Low },
    { label: 'Medium', value: FanSpeed.Medium },
    { label: 'High', value: FanSpeed.High },
  ];

  /* ─────────────── Unique IDs ─────────────── */

  private readonly _uid = Math.random().toString(36).slice(2, 9);
  readonly powerId = `fan-power-${this._uid}`;

  /* ─────────────── Computed visual state ─────────────── */

  readonly isOn = computed(() => this.powerState() === PowerState.On);

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


  bladePath(): string {
    const tipR = BACKDROP_R - 18;
    const baseHalfWidth = 8;
    const tipHalfWidth = 14;
    const offset = 6;

    return `M ${CX - baseHalfWidth} ${CY}
            Q ${CX - baseHalfWidth - offset} ${CY - tipR / 2} ${CX - tipHalfWidth} ${CY - tipR}
            L ${CX + tipHalfWidth} ${CY - tipR}
            Q ${CX + baseHalfWidth + offset} ${CY - tipR / 2} ${CX + baseHalfWidth} ${CY}
            Z`;
  }

  /* ─────────────── Event handlers ─────────────── */

  onPowerToggle(next: boolean): void {
    this.powerStateChange.emit(next ? PowerState.On : PowerState.Off);
  }

  onSpeedChange(speed: FanSpeed): void {
    if (!this.isOn()) return;
    if (speed !== this.speed()) {
      this.speedChange.emit(speed);
    }
  }
}
