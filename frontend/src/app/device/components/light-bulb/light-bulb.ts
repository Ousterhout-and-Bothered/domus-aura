import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { CardModule } from 'primeng/card';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { SliderModule } from 'primeng/slider';
import { ColorPickerModule } from 'primeng/colorpicker';

import { PowerState } from '../../models/device';

/* ─────────────── Constants ─────────────── */

const VIEWBOX = 200;
const CX = 100;
const CY = 100;
const BULB_R = 38;
const GLOW_RING_COUNT = 4;

/**
 * Convert a hex color (#RRGGBB) to an rgba() string with the given alpha.
 * Returns the original hex if parsing fails.
 */
function hexWithAlpha(hex: string, alpha: number): string {
  const match = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
  if (!match) return hex;
  const r = parseInt(match[1], 16);
  const g = parseInt(match[2], 16);
  const b = parseInt(match[3], 16);
  const a = Math.max(0, Math.min(1, alpha));
  return `rgba(${r}, ${g}, ${b}, ${a})`;
}

@Component({
  selector: 'aura-light-bulb',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardModule,
    ToggleSwitchModule,
    SliderModule,
    ColorPickerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-card styleClass="light-card" [style.--light-glow]="cardGlowColor()">
      <ng-template pTemplate="header">
        <div class="light-card-head">
          <h3 class="light-name">{{ name() }}</h3>
          <p class="light-loc">{{ location() }}</p>
        </div>
      </ng-template>

      <div class="bulb-stage">
        <svg
          class="bulb-svg"
          [attr.viewBox]="'0 0 ' + viewBox + ' ' + viewBox"
          [attr.aria-label]="
            'Light. Power ' + powerState() +
            ', brightness ' + brightness() + '%, color ' + colorHex()
          "
          role="img"
        >
          <defs>
            <radialGradient [attr.id]="bulbGradientId" cx="0.5" cy="0.5" r="0.5">
              <stop offset="0%" [attr.stop-color]="centerColor()"/>
              <stop offset="60%" [attr.stop-color]="colorHex()"/>
              <stop offset="100%" [attr.stop-color]="edgeColor()"/>
            </radialGradient>
          </defs>

          <defs>
            <linearGradient [attr.id]="roomGradientId" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" [attr.stop-color]="roomTopColor()"/>
              <stop offset="100%" [attr.stop-color]="roomBottomColor()"/>
            </linearGradient>
          </defs>


          @if (isOn()) {
            @for (ring of glowRings(); track ring.index) {
              <circle
                class="glow-ring"
                [attr.cx]="CX"
                [attr.cy]="CY"
                [attr.r]="ring.radius"
                [attr.fill]="colorHex()"
                [attr.opacity]="ring.opacity"
              />
            }
          }

          <line
            [attr.x1]="CX"
            [attr.y1]="0"
            [attr.x2]="CX"
            [attr.y2]="CY - BULB_R"
            stroke="#3a342b"
            stroke-width="1.5"
            opacity="0.5"
          />

          <circle
            class="bulb"
            [class.bulb-on]="isOn()"
            [attr.cx]="CX"
            [attr.cy]="CY"
            [attr.r]="BULB_R"
            [attr.fill]="isOn() ? 'url(#' + bulbGradientId + ')' : '#D4D0C8'"
          />

          @if (isOn()) {
            <g class="filament" [attr.opacity]="filamentOpacity()">
              <path
                [attr.d]="
                  'M ' + (CX - 14) + ' ' + (CY - 4) +
                  ' Q ' + CX + ' ' + (CY + 14) + ' ' +
                  (CX + 14) + ' ' + (CY - 4)
                "
                fill="none"
                stroke="#FFF8E0"
                stroke-width="5"
                stroke-linecap="round"
              />
              <line
                [attr.x1]="CX - 16" [attr.y1]="CY - 4"
                [attr.x2]="CX - 14" [attr.y2]="CY - 4"
                stroke="#FFF8E0" stroke-width="2" stroke-linecap="round"
              />
              <line
                [attr.x1]="CX + 14" [attr.y1]="CY - 4"
                [attr.x2]="CX + 16" [attr.y2]="CY - 4"
                stroke="#FFF8E0" stroke-width="2" stroke-linecap="round"
              />
            </g>
          }

          <rect
            class="bulb-base"
            [attr.x]="CX - 14"
            [attr.y]="CY + BULB_R - 6"
            width="28" height="14" rx="2"
            fill="#3a342b"
          />
          <rect
            [attr.x]="CX - 12" [attr.y]="CY + BULB_R + 8"
            width="24" height="3" rx="1"
            fill="#3a342b" opacity="0.6"
          />
          <rect
            [attr.x]="CX - 12" [attr.y]="CY + BULB_R + 13"
            width="24" height="3" rx="1"
            fill="#3a342b" opacity="0.6"
          />
        </svg>

        <div class="light-readout">
          <div class="light-state-label">{{ stateLabel() }}</div>
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

      <div class="control-row brightness-row" [class.is-disabled]="!isOn()">
        <span class="control-label">Brightness</span>
        <p-slider
          [min]="10"
          [max]="100"
          [step]="1"
          [disabled]="!isOn()"
          [ngModel]="localBrightness()"
          (ngModelChange)="onBrightnessDrag($event)"
          (onSlideEnd)="onBrightnessCommit($event)"
          styleClass="brightness-slider"
        />
        <span class="control-value">{{ localBrightness() }}%</span>
      </div>

      <!-- Color: just the picker, sized prominently to match the cinematic aesthetic -->
      <div class="control-row color-row" [class.is-disabled]="!isOn()">
        <span class="control-label">Color</span>
        <p-colorpicker
          format="hex"
          [ngModel]="colorHex()"
          (onChange)="onColorCustom($event)"
          [disabled]="!isOn()"
          appendTo="body"></p-colorpicker>
        <span class="control-value">{{ colorHex().toUpperCase() }}</span>
      </div>
    </p-card>
  `,
  styleUrl: './light-bulb.scss',
})
export class LightBulb {
  /* ─────────────── Inputs / outputs ─────────────── */

  readonly name = input.required<string>();
  readonly location = input.required<string>();
  readonly powerState = input.required<PowerState>();
  readonly brightness = input.required<number>();
  readonly colorHex = input.required<string>();

  readonly powerStateChange = output<PowerState>();
  readonly brightnessChange = output<number>();
  readonly colorHexChange = output<string>();

  /* ─────────────── Template constants ─────────────── */

  readonly viewBox = VIEWBOX;
  readonly CX = CX;
  readonly CY = CY;
  readonly BULB_R = BULB_R;

  /* ─────────────── Unique IDs ─────────────── */

  private readonly _uid = Math.random().toString(36).slice(2, 9);
  readonly bulbGradientId = `bulb-grad-${this._uid}`;
  readonly roomGradientId = `room-grad-${this._uid}`;
  readonly powerId = `power-${this._uid}`;

  /* ─────────────── Local slider state ───────────────
   *
   * The slider drags at high frequency. We mirror the input value into
   * a writable signal so the readout updates with the thumb in real time,
   * but only emit upstream (firing a PUT) when the user lets go.
   */
  private readonly _localBrightness = signal(0);
  readonly localBrightness = this._localBrightness.asReadonly();

  constructor() {
    // Sync local ← input whenever input changes.
    effect(() => {
      this._localBrightness.set(this.brightness());
    });
  }

  /* ─────────────── Computed visual state ─────────────── */

  readonly isOn = computed(() => this.powerState() === PowerState.On);

  readonly stateLabel = computed(() => {
    if (!this.isOn()) return 'Off';
    return `${this.brightness()}%`;
  });

  readonly roomTopColor = computed(() =>
    this.isOn() ? '#F5E8C8' : '#D9CAA0'
  );

  readonly roomBottomColor = computed(() =>
    this.isOn() ? '#C9A878' : '#9B7843'
  );

  readonly glowRings = computed(() => {
    const intensity = this.brightness() / 100;
    const rings = [];
    for (let i = 0; i < GLOW_RING_COUNT; i++) {
      const t = (i + 1) / GLOW_RING_COUNT;
      rings.push({
        index: i,
        radius: BULB_R + 12 + i * 14,
        opacity: (1 - t) * 0.35 * intensity,
      });
    }
    return rings;
  });

  readonly centerColor = computed(() => {
    const intensity = this.brightness() / 100;
    const alpha = 0.3 + intensity * 0.7;
    if (this.brightness() > 70) {
      return hexWithAlpha('#FFFFFF', alpha);
    }
    return hexWithAlpha(this.colorHex(), alpha);
  });

  readonly edgeColor = computed(() => {
    const intensity = this.brightness() / 100;
    return hexWithAlpha(this.colorHex(), intensity * 0.7);
  });

  readonly filamentOpacity = computed(() =>
    Math.min(1, this.brightness() / 100 + 0.2)
  );

  readonly cardGlowColor = computed(() => {
    if (!this.isOn()) return 'transparent';
    const intensity = this.brightness() / 100;
    return hexWithAlpha(this.colorHex(), intensity * 0.5);
  });

  /* ─────────────── Event handlers ─────────────── */

  onPowerToggle(next: boolean): void {
    // PowerState is an enum, not a string union — must use enum members.
    this.powerStateChange.emit(next ? PowerState.On : PowerState.Off);
  }

  onBrightnessDrag(value: number): void {
    this._localBrightness.set(value);
  }

  onBrightnessCommit(event: { value?: number }): void {
    const value = event.value ?? this._localBrightness();
    if (!Number.isNaN(value) && value !== this.brightness()) {
      this.brightnessChange.emit(value);
    }
  }

  onColorCustom(event: { value: string | object | null }): void {
    if (!this.isOn()) return;
    const raw = event.value;
    if (!raw || typeof raw !== 'string') return;
    const hex = raw.startsWith('#') ? raw : `#${raw}`;
    if (hex.toUpperCase() !== this.colorHex().toUpperCase()) {
      this.colorHexChange.emit(hex.toUpperCase());
    }
  }
}
