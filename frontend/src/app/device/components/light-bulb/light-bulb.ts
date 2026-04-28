import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { PowerState } from '../../models/device';

/* ─────────────── Constants ─────────────── */

const VIEWBOX = 200;
const CX = 100;
const CY = 100;
const BULB_R = 38;
const GLOW_RING_COUNT = 4;

/** Curated palette — quick-pick swatches. Custom hex still works via the input. */
const COLOR_PRESETS = [
  '#FFE4B5', // warm white
  '#FFD580', // amber
  '#FF8800', // orange
  '#FF4D4D', // red
  '#FF66CC', // pink
  '#9B59FF', // purple
  '#5EB0FF', // sky blue
  '#5EFFC9', // mint
  '#A8FF66', // lime
];

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
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="light-card" [style.--light-glow]="cardGlowColor()">
      <header>
        <h3 class="light-name">{{ name() }}</h3>
        <p class="light-loc">{{ location() }}</p>
      </header>

      <div class="bulb-stage">
        <svg
          class="bulb-svg"
          [attr.viewBox]="'0 0 ' + viewBox + ' ' + viewBox"
          [attr.aria-label]="'Light. Power ' + powerState() + ', brightness ' + brightness() + '%, color ' + colorHex()"
          role="img"
        >
          <defs>
            <radialGradient [attr.id]="bulbGradientId" cx="0.5" cy="0.5" r="0.5">
              <stop offset="0%" [attr.stop-color]="centerColor()" />
              <stop offset="60%" [attr.stop-color]="colorHex()" />
              <stop offset="100%" [attr.stop-color]="edgeColor()" />
            </radialGradient>
          </defs>

          <defs>
            <linearGradient [attr.id]="roomGradientId" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" [attr.stop-color]="roomTopColor()" />
              <stop offset="100%" [attr.stop-color]="roomBottomColor()" />
            </linearGradient>
          </defs>

          <!-- Concentric glow rings (only when on) -->
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

          <!-- Pendant cord -->
          <line
            [attr.x1]="CX"
            [attr.y1]="0"
            [attr.x2]="CX"
            [attr.y2]="CY - BULB_R"
            stroke="#3a342b"
            stroke-width="1.5"
            opacity="0.5"
          />

          <!-- Bulb itself -->
          <circle
            class="bulb"
            [class.bulb-on]="isOn()"
            [attr.cx]="CX"
            [attr.cy]="CY"
            [attr.r]="BULB_R"
            [attr.fill]="isOn() ? 'url(#' + bulbGradientId + ')' : '#D4D0C8'"
          />

          <!-- Edison filament (decorative) — visible only when on -->
          @if (isOn()) {
            <g class="filament" [attr.opacity]="filamentOpacity()">
              <path
                [attr.d]="'M ' + (CX - 14) + ' ' + (CY - 4) + ' Q ' + CX + ' ' + (CY + 14) + ' ' + (CX + 14) + ' ' + (CY - 4)"
                fill="none"
                stroke="#FFF8E0"
                stroke-width="5"
                stroke-linecap="round"
              />
              <line [attr.x1]="CX - 16" [attr.y1]="CY - 4" [attr.x2]="CX - 14" [attr.y2]="CY - 4"
                    stroke="#FFF8E0" stroke-width="2" stroke-linecap="round" />
              <line [attr.x1]="CX + 14" [attr.y1]="CY - 4" [attr.x2]="CX + 16" [attr.y2]="CY - 4"
                    stroke="#FFF8E0" stroke-width="2" stroke-linecap="round" />
            </g>
          }

          <!-- Bulb base (the screw cap) -->
          <rect
            class="bulb-base"
            [attr.x]="CX - 14"
            [attr.y]="CY + BULB_R - 6"
            width="28"
            height="14"
            rx="2"
            fill="#3a342b"
          />
          <rect
            [attr.x]="CX - 12"
            [attr.y]="CY + BULB_R + 8"
            width="24"
            height="3"
            rx="1"
            fill="#3a342b"
            opacity="0.6"
          />
          <rect
            [attr.x]="CX - 12"
            [attr.y]="CY + BULB_R + 13"
            width="24"
            height="3"
            rx="1"
            fill="#3a342b"
            opacity="0.6"
          />
        </svg>

        <div class="light-readout">
          <div class="light-state-label">{{ stateLabel() }}</div>
        </div>
      </div>

      <!-- Power button -->
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

      <!-- Brightness slider -->
      <div class="brightness-row" [class.disabled]="!isOn()">
        <label [attr.for]="brightnessId">Brightness</label>
        <input
          type="range"
          [id]="brightnessId"
          min="10"
          max="100"
          step="1"
          [value]="brightness()"
          [disabled]="!isOn()"
          (input)="onBrightnessInput($event)"
        />
        <span class="val">{{ brightness() }}%</span>
      </div>

      <!-- Color preset swatches -->
      <div class="color-row" [class.disabled]="!isOn()">
        <label>Color</label>
        <div class="color-swatches">
          @for (preset of COLOR_PRESETS; track preset) {
            <button
              type="button"
              class="color-swatch"
              [class.active]="colorHex().toUpperCase() === preset.toUpperCase()"
              [style.background-color]="preset"
              [disabled]="!isOn()"
              [attr.aria-label]="'Set color to ' + preset"
              (click)="onColorClick(preset)"
            ></button>
          }
        </div>
      </div>
    </article>
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
  readonly COLOR_PRESETS = COLOR_PRESETS;

  /* ─────────────── Unique IDs ─────────────── */

  private readonly _uid = Math.random().toString(36).slice(2, 9);
  readonly bulbGradientId = `bulb-grad-${this._uid}`;
  readonly roomGradientId = `room-grad-${this._uid}`;
  readonly brightnessId = `brightness-${this._uid}`;

  /* ─────────────── Computed visual state ─────────────── */

  readonly isOn = computed(() => this.powerState() === 'On');

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

  /**
   * Concentric glow rings outward from the bulb.
   * Each ring's opacity scales with brightness — at low brightness, only the
   * inner rings are visible; at full brightness, all rings glow softly.
   */
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
    const alpha = 0.3 + intensity * 0.7;  // ranges from 0.3 (at 0%) to 1.0 (at 100%)
    if (this.brightness() > 70) {
      return hexWithAlpha('#FFFFFF', alpha);
    }
    return hexWithAlpha(this.colorHex(), alpha);
  });

  readonly edgeColor = computed(() => {
    // Edge fades more aggressively than center
    const intensity = this.brightness() / 100;
    return hexWithAlpha(this.colorHex(), intensity * 0.7);
  });

  /** Filament glows brighter when brightness is higher. */
  readonly filamentOpacity = computed(() => {
    return Math.min(1, this.brightness() / 100 + 0.2);
  });

  /**
   * Card glow color — used as a CSS variable for a subtle radial halo
   * inside the card. Empty string when off so the halo disappears.
   */
  readonly cardGlowColor = computed(() => {
    if (!this.isOn()) return 'transparent';
    // Convert hex to rgba with brightness-driven alpha
    const intensity = this.brightness() / 100;
    return hexWithAlpha(this.colorHex(), intensity * 0.5);
  });

  /* ─────────────── Event handlers ─────────────── */

  onPowerClick(): void {
    const next = this.isOn() ? 'Off' : 'On';
    this.powerStateChange.emit(next as PowerState);
  }

  onBrightnessInput(event: Event): void {
    const value = parseInt((event.target as HTMLInputElement).value, 10);
    if (!Number.isNaN(value) && value !== this.brightness()) {
      this.brightnessChange.emit(value);
    }
  }

  onColorClick(hex: string): void {
    if (!this.isOn()) return;
    if (hex.toUpperCase() !== this.colorHex().toUpperCase()) {
      this.colorHexChange.emit(hex);
    }
  }
}
