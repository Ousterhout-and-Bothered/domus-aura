import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  HostListener,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { ThermostatMode, ThermostatState } from '../../models/device-types';

/* ─────────────── Geometry constants ─────────────── */

const TEMP_MIN = 60;       // matches Domain Thermostat MinTemperature
const TEMP_MAX = 80;       // matches Domain Thermostat MaxTemperature
const ARC_START = 270;     // 9 o'clock (left edge)
const ARC_END = 90;       // 3 o'clock (right edge) — together with START makes a half circle going over the top
const ARC_SPAN = 180;      // ARC_END - ARC_START
const VIEWBOX_W = 380;
const VIEWBOX_H = 220;
const CX = 190;
const CY = 178;            // pivot near the bottom of the viewbox so the half-circle has room above
const OUTER_R = 160;       // outer edge of the band
const INNER_R = 0;         // 0 = filled half-disc (no hollow center)
const TICK_TEMPS = Array.from({ length: 21 }, (_, i) => 60 + i);
const TICK_WIDTH = 3;
const TICK_LENGTH = 14;
const TICK_INNER_R = OUTER_R - 14;   // how far in the ticks/needle reach
const TICK_OUTER_R = OUTER_R;        // outer edge (matches the band edge)

/**
 * Speedometer-style thermostat gauge. Renders a half-donut band whose
 * color reflects the active state, a chunky needle pointing at the
 * desired temperature, embedded tick marks at every 5°F, and a center
 * readout for desired temp + state label.
 *
 * The needle is draggable along the arc; a slider below provides
 * keyboard/precise input. Mode buttons emit a separate event for
 * setting Heat/Cool/Auto.
 *
 * Temperature ranges match the backend authoritatively:
 *   - Desired:  60-80°F (Domain clamp + DeviceCommandRequestValidator)
 *   - Ambient marker visible for 60-80°F; off-dial values just hide
 *     the marker (the readout still shows the real value)
 */
@Component({
  selector: 'aura-thermostat-gauge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="thermo-card">
      <header>
        <h3 class="thermo-name">{{ name() }}</h3>
        <p class="thermo-loc">{{ location() }}</p>
      </header>

      <div class="gauge-stage">
        <svg
          #svgEl
          class="gauge-svg"
          [attr.viewBox]="'0 0 ' + viewBoxW + ' ' + viewBoxH"
          [attr.aria-label]="'Thermostat dial. Desired ' + desiredTemperature() + ' degrees, currently ' + activeState()"
          role="img"
        >
          <defs>
            <mask [attr.id]="maskId">
              <rect [attr.x]="0" [attr.y]="0" [attr.width]="viewBoxW" [attr.height]="viewBoxH" fill="white" />
              @for (tick of ticks(); track tick.temp) {
                <rect
                  [attr.x]="tick.x - tickWidth / 2"
                  [attr.y]="tick.y - tickLength / 2"
                  [attr.width]="tickWidth"
                  [attr.height]="tickLength"
                  rx="1"
                  fill="black"
                  [attr.transform]="'rotate(' + (tick.angle - 90) + ' ' + tick.x + ' ' + tick.y + ')'"
                />
              }
            </mask>
          </defs>
          <path
            class="gauge-backdrop"
            [attr.d]="bandPath"
            fill="#FBF8F3"
          />
          <path
            class="band"
            [attr.d]="bandPath"
            [attr.fill]="theme().bandFill"
            [attr.mask]="'url(#' + maskId + ')'"
          />

          @for (tick of outerTicks(); track tick.temp) {
            <line
              [attr.x1]="tick.x1"
              [attr.y1]="tick.y1"
              [attr.x2]="tick.x2"
              [attr.y2]="tick.y2"
              stroke="#3a342b"
              stroke-width="2"
              stroke-linecap="round"
              opacity="0.6"
            />
          }

          @if (ambientMarker(); as marker) {
            <circle
              [attr.cx]="marker.x"
              [attr.cy]="marker.y"
              r="5"
              fill="#3a342b"
              opacity="0.7"
            />
          }

          <g
            class="needle-group"
            [attr.transform]="needleTransform()"
            (pointerdown)="onNeedlePointerDown($event)"
          >
            <path
              class="needle"
              [attr.d]="needlePath()"
              [attr.fill]="theme().needleFill"
            />
            <circle [attr.cx]="CX" [attr.cy]="CY" r="6" fill="var(--aura-text)" />
          </g>
        </svg>

        <div class="gauge-readout">
          <div>
            <span class="gauge-temp-big">{{ desiredTemperature() }}</span>
            <span class="gauge-temp-unit">°F</span>
          </div>
          <div class="gauge-state" [class]="'gauge-state-' + activeState()">
            {{ activeStateLabel() }}
          </div>
        </div>
      </div>

      <p class="ambient-readout">
        Ambient <strong>{{ ambientTemperature() }}°F</strong>
        · {{ ambientContext() }}
      </p>

      <div class="controls-row">
        <label [attr.for]="setpointId">Set point</label>
        <input
          type="range"
          [id]="setpointId"
          [min]="TEMP_MIN"
          [max]="TEMP_MAX"
          step="1"
          [value]="desiredTemperature()"
          (input)="onSetpointInput($event)"
        />
        <span class="val">{{ desiredTemperature() }}°</span>
      </div>

      <div class="mode-row">
        @for (m of MODES; track m) {
          <button
            type="button"
            class="mode-btn"
            [class.active]="mode() === m"
            (click)="onModeClick(m)"
          >
            {{ m }}
          </button>
        }
      </div>
    </article>
  `,
  styleUrl: './thermostat-gauge.scss',
})
export class ThermostatGauge {
  /* ─────────────── Inputs / outputs ─────────────── */

  readonly name = input.required<string>();
  readonly location = input.required<string>();
  readonly desiredTemperature = input.required<number>();
  readonly ambientTemperature = input.required<number>();
  readonly mode = input.required<ThermostatMode>();
  readonly state = input.required<ThermostatState>();

  readonly desiredTemperatureChange = output<number>();
  readonly modeChange = output<ThermostatMode>();

  /* ─────────────── Template constants ─────────────── */

  readonly TEMP_MIN = TEMP_MIN;
  readonly TEMP_MAX = TEMP_MAX;
  readonly viewBoxW = VIEWBOX_W;
  readonly viewBoxH = VIEWBOX_H;
  readonly tickWidth = TICK_WIDTH;
  readonly tickLength = TICK_LENGTH;
  readonly bandPath = buildBandPath();
  readonly CX = CX;
  readonly CY = CY;
  readonly MODES: ThermostatMode[] = [
    'Heat' as ThermostatMode,
    'Cool' as ThermostatMode,
    'Auto' as ThermostatMode,
  ];

  /* ─────────────── Unique IDs ─────────────── */

  private readonly _uid = Math.random().toString(36).slice(2, 9);
  readonly maskId = `band-mask-${this._uid}`;
  readonly setpointId = `setpoint-${this._uid}`;

  /* ─────────────── Computed visual state ─────────────── */

  readonly ticks = computed(() =>
    TICK_TEMPS.map((temp) => {
      const angle = tempToAngle(temp);
      const pt = angleToPoint(angle, (OUTER_R + INNER_R) / 2);
      return { temp, angle, x: pt.x, y: pt.y };
    })
  );

  readonly outerTicks = computed(() =>
    TICK_TEMPS.map((temp) => {
      const angle = tempToAngle(temp);
      const inner = angleToPoint(angle, TICK_INNER_R);
      const outer = angleToPoint(angle, TICK_OUTER_R);
      return { temp, angle, x1: inner.x, y1: inner.y, x2: outer.x, y2: outer.y };
    })
  );

  readonly needleTransform = computed(() => {
    const angle = tempToAngle(this.desiredTemperature());
    return `rotate(${angle} ${CX} ${CY})`;
  });

  readonly needlePath = computed(() => {
    const tipR = TICK_INNER_R;        // ← needle tip = inner end of ticks
    const baseHalfWidth = 6;
    const tipHalfWidth = 2;

    return `M ${CX - baseHalfWidth} ${CY}
          L ${CX - tipHalfWidth} ${CY - tipR}
          L ${CX + tipHalfWidth} ${CY - tipR}
          L ${CX + baseHalfWidth} ${CY}
          Z`;
  });

  readonly ambientMarker = computed(() => {
    const t = this.ambientTemperature();
    if (t < TEMP_MIN || t > TEMP_MAX) return null;
    const pt = angleToPoint(tempToAngle(t), (OUTER_R + INNER_R) / 2);
    return { x: pt.x, y: pt.y };
  });

  /**
   * Use the backend's authoritative `state` field rather than recomputing.
   * Off → idle for the visual.
   */
  readonly activeState = computed<'heating' | 'cooling' | 'idle'>(() => {
    const s = this.state();
    if (s === 'Heating') return 'heating';
    if (s === 'Cooling') return 'cooling';
    return 'idle';
  });

  readonly activeStateLabel = computed(() => {
    const s = this.activeState();
    return s.charAt(0).toUpperCase() + s.slice(1);
  });

  readonly ambientContext = computed(() => {
    switch (this.activeState()) {
      case 'heating': return 'warming the room';
      case 'cooling': return 'cooling the room';
      default: return 'at set point';
    }
  });

  readonly theme = computed(() => {
    switch (this.activeState()) {
      case 'heating': return { bandFill: '#FED7AA', needleFill: '#C2410C' };
      case 'cooling': return { bandFill: '#DBEAFE', needleFill: '#3B82F6' };
      default:        return { bandFill: '#F1EFE8', needleFill: '#888780' };
    }
  });

  /* ─────────────── Drag interaction ─────────────── */

  private readonly svgEl = viewChild.required<ElementRef<SVGSVGElement>>('svgEl');
  private readonly _dragging = signal(false);
  private _activePointerId: number | null = null;

  onNeedlePointerDown(event: PointerEvent): void {
    event.preventDefault();
    const target = event.currentTarget as SVGElement;
    target.setPointerCapture(event.pointerId);
    this._activePointerId = event.pointerId;
    this._dragging.set(true);
  }

  @HostListener('document:pointermove', ['$event'])
  onDocumentPointerMove(event: PointerEvent): void {
    if (!this._dragging() || event.pointerId !== this._activePointerId) return;

    const svg = this.svgEl().nativeElement;
    const rect = svg.getBoundingClientRect();
    const svgX = ((event.clientX - rect.left) / rect.width) * VIEWBOX_W;
    const svgY = ((event.clientY - rect.top) / rect.height) * VIEWBOX_H;

    const newTemp = pointToTemp(svgX, svgY);
    if (newTemp !== this.desiredTemperature()) {
      this.desiredTemperatureChange.emit(newTemp);
    }
  }

  @HostListener('document:pointerup', ['$event'])
  onDocumentPointerUp(event: PointerEvent): void {
    if (event.pointerId !== this._activePointerId) return;
    this._dragging.set(false);
    this._activePointerId = null;
  }

  /* ─────────────── Slider + mode buttons ─────────────── */

  onSetpointInput(event: Event): void {
    const value = parseInt((event.target as HTMLInputElement).value, 10);
    if (!Number.isNaN(value) && value !== this.desiredTemperature()) {
      this.desiredTemperatureChange.emit(value);
    }
  }

  onModeClick(mode: ThermostatMode): void {
    if (mode !== this.mode()) {
      this.modeChange.emit(mode);
    }
  }
}

/* ─────────────── Pure geometry helpers (top-level) ─────────────── */

function angleToPoint(angleDeg: number, radius: number): { x: number; y: number } {
  const rad = ((angleDeg - 90) * Math.PI) / 180;
  return { x: CX + Math.cos(rad) * radius, y: CY + Math.sin(rad) * radius };
}

function tempToAngle(t: number): number {
  const clamped = Math.max(TEMP_MIN, Math.min(TEMP_MAX, t));
  return ARC_START + ((clamped - TEMP_MIN) / (TEMP_MAX - TEMP_MIN)) * ARC_SPAN;
}

function buildBandPath(): string {
  const startOuter = angleToPoint(ARC_START, OUTER_R);
  const endOuter = angleToPoint(ARC_END, OUTER_R);
  const endInner = angleToPoint(ARC_END, INNER_R);
  const startInner = angleToPoint(ARC_START, INNER_R);
  return `M ${startOuter.x} ${startOuter.y}
          A ${OUTER_R} ${OUTER_R} 0 1 1 ${endOuter.x} ${endOuter.y}
          L ${endInner.x} ${endInner.y}
          L ${startInner.x} ${startInner.y}
          Z`;
}

/**
 * Convert SVG pointer position to integer temperature, clamped to range.
 * Used during needle drag.
 */
/**
 * Convert SVG pointer position to integer temperature, clamped to range.
 * Used during needle drag.
 *
 * Handles dial layouts where ARC crosses 0°/360° (e.g. ARC_START=270, ARC_END=90).
 */
function pointToTemp(svgX: number, svgY: number): number {
  const dx = svgX - CX;
  const dy = svgY - CY;

  // Convert pointer position to dial angle, in the same coordinate system
  // tempToAngle uses (0° = 3 o'clock, growing clockwise).
  let angleDeg = (Math.atan2(dy, dx) * 180) / Math.PI + 90;
  if (angleDeg < 0) angleDeg += 360;

  // Compute how far around the arc the pointer is, as a fraction 0..1.
  // We measure clockwise from ARC_START. If ARC_END < ARC_START (wrap-around
  // case like 270→90), unwrap by adding 360 to the relative angle.
  let relative = angleDeg - ARC_START;
  if (relative < 0) relative += 360;

  // Snap to nearest end if cursor falls outside the arc span.
  if (relative > ARC_SPAN) {
    // Closer to the end or to the start?
    const distFromEnd = relative - ARC_SPAN;
    const distFromStart = 360 - relative;
    relative = distFromEnd < distFromStart ? ARC_SPAN : 0;
  }

  const fraction = relative / ARC_SPAN;
  const t = TEMP_MIN + fraction * (TEMP_MAX - TEMP_MIN);
  return Math.round(Math.max(TEMP_MIN, Math.min(TEMP_MAX, t)));
}
