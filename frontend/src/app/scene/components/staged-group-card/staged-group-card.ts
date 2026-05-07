import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';

import { DeviceType, PowerState } from '../../../device/models/device';
import {
  AnyDevice,
  DoorLockState,
  FanSpeed,
  ThermostatMode,
  ThermostatState,
} from '../../../device/models/device-types';
import { SceneActionRequest } from '../../models/scene';
import {
  StagedGroupTarget,
  makeStagedGroupTarget,
} from '../../models/staged-group-target';

import { LightBulb } from '../../../device/components/light-bulb/light-bulb';
import { FanSpinning } from '../../../device/components/fan-spinning/fan-spinning';
import { ThermostatGauge } from '../../../device/components/thermostat-gauge/thermostat-gauge';
import { DoorLock } from '../../../device/components/door-lock/door-lock';

/**
 * Local discriminated-union of the staged values for a group target.
 * Mirrors StagedDeviceState in StagedTargetCard but represents an
 * action template applied uniformly to every device the group resolves
 * to, rather than the state of one specific device.
 */
type StagedGroupState =
  | {
  type: DeviceType.Light;
  powerState: PowerState;
  brightness: number;
  colorHex: string;
}
  | {
  type: DeviceType.Fan;
  powerState: PowerState;
  speed: FanSpeed;
}
  | {
  type: DeviceType.Thermostat;
  state: ThermostatState;
  mode: ThermostatMode;
  desiredTemperature: number;
}
  | {
  type: DeviceType.DoorLock;
  lockState: DoorLockState;
};

type TouchedProperty =
  | 'power'
  | 'brightness'
  | 'color'
  | 'speed'
  | 'mode'
  | 'desiredTemperature'
  | 'lockState';

const ANY_LOCATION_VALUE = '__ANY__';

interface LocationOption {
  label: string;
  value: string;
}

/**
 * Defaults used when the group resolves to zero matching devices and
 * we have no device to use as a visualization template. Kept centralized
 * so all four device types stay in sync. Values chosen to be inert /
 * neutral — no surprises if the user opens an empty-group card.
 */
const FALLBACK_DEFAULTS = {
  light: { powerState: PowerState.Off, brightness: 100, colorHex: '#FFFFFF' },
  fan: { powerState: PowerState.Off, speed: FanSpeed.Low },
  thermostat: {
    state: ThermostatState.Off,
    mode: ThermostatMode.Auto,
    desiredTemperature: 72,
    ambientTemperature: 70,
  },
  doorLock: { lockState: DoorLockState.Locked },
} as const;

/**
 * One staged group target inside the scene editor dialog. Represents
 * a rule like "all Lights" or "all Fans in Bedroom" — resolved to
 * matching devices at execute time, not save time.
 *
 * Differs from StagedTargetCard in three meaningful ways:
 *   1. No specific device — the visualization renders an action
 *      template using the first matching device as a stand-in (or
 *      synthesized defaults if no match).
 *   2. Includes a location dropdown that scopes the rule. Changing
 *      it emits groupChanged so the parent can update its stagedGroups
 *      array (the group's composite id changes with location).
 *   3. Shows a list of currently-matching devices so the user knows
 *      what the rule will hit.
 *
 * Action-tracking logic (touched set, configure-once-applies-uniformly)
 * is identical to StagedTargetCard's. On save, toActions emits one
 * SceneActionRequest per touched property with deviceType + location
 * set, deviceId left null.
 */
@Component({
  selector: 'aura-staged-group-card',
  standalone: true,
  imports: [FormsModule, SelectModule, LightBulb, FanSpinning, ThermostatGauge, DoorLock],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="staged-group" [class.is-expanded]="expanded()">
      <header class="staged-group-head">
        <div class="staged-group-titles">
          <h3 class="staged-group-name">{{ groupTitle() }}</h3>
          <p class="staged-group-meta">{{ groupMeta() }}</p>
        </div>
        <span class="staged-group-badge">{{ actionCount() }} actions</span>
        <button
          type="button"
          class="staged-group-remove"
          [attr.aria-label]="'Remove ' + groupTitle() + ' from scene'"
          (click)="remove.emit()"
        >
          <i class="pi pi-times"></i>
        </button>
      </header>

      <div class="staged-group-summary">
        <p class="summary-text">{{ summary() }}</p>
        <button
          type="button"
          class="staged-group-configure"
          (click)="onToggleExpanded()"
        >
          <i class="pi pi-pencil"></i>
          Configure
        </button>
      </div>

      <div class="staged-group-body">
        <div class="staged-group-body-actions">
          <button
            type="button"
            class="staged-group-done"
            (click)="onToggleExpanded()"
          >
            <i class="pi pi-check"></i>
            Done
          </button>
        </div>

        <div class="staged-group-location">
          <label class="staged-group-location-label">Location scope</label>
          <p-select
            [options]="locationOptions()"
            [ngModel]="selectedLocationValue()"
            (ngModelChange)="onLocationChange($event)"
            optionLabel="label"
            optionValue="value"
            appendTo="body"
            styleClass="staged-group-location-select"
          />
        </div>

        <div class="staged-group-template-section">
          <p class="staged-group-section-label">Configure for the group</p>
          @switch (group().deviceType) {
            @case (DeviceType.Light) {
              @let s = stagedState();
              @if (s !== null && s.type === DeviceType.Light) {
                <aura-light-bulb
                  [name]="templateName()"
                  [location]="templateLocation()"
                  [powerState]="s.powerState"
                  [brightness]="s.brightness"
                  [colorHex]="s.colorHex"
                  (powerStateChange)="onLightPowerChange($event)"
                  (brightnessChange)="onLightBrightnessChange($event)"
                  (colorHexChange)="onLightColorChange($event)"
                />
              }
            }

            @case (DeviceType.Fan) {
              @let s = stagedState();
              @if (s !== null && s.type === DeviceType.Fan) {
                <aura-fan-spinning
                  [name]="templateName()"
                  [location]="templateLocation()"
                  [powerState]="s.powerState"
                  [speed]="s.speed"
                  (powerStateChange)="onFanPowerChange($event)"
                  (speedChange)="onFanSpeedChange($event)"
                />
              }
            }

            @case (DeviceType.Thermostat) {
              @let s = stagedState();
              @if (s !== null && s.type === DeviceType.Thermostat) {
                <aura-thermostat-gauge
                  [name]="templateName()"
                  [location]="templateLocation()"
                  [desiredTemperature]="s.desiredTemperature"
                  [ambientTemperature]="templateAmbient()"
                  [mode]="s.mode"
                  [state]="s.state"
                  (stateChange)="onThermostatStateChange($event)"
                  (desiredTemperatureChange)="onThermostatDesiredTempChange($event)"
                  (modeChange)="onThermostatModeChange($event)"
                />
              }
            }

            @case (DeviceType.DoorLock) {
              @let s = stagedState();
              @if (s !== null && s.type === DeviceType.DoorLock) {
                <aura-door-lock
                  [name]="templateName()"
                  [location]="templateLocation()"
                  [lockState]="s.lockState"
                  (lockStateChange)="onLockStateChange($event)"
                />
              }
            }
          }
        </div>

        <div class="staged-group-matches">
          <p class="staged-group-section-label">
            Matches ({{ matchedDevices().length }})
          </p>
          @if (matchedDevices().length === 0) {
            <p class="staged-group-no-matches">
              No devices currently match this rule.
            </p>
          } @else {
            <ul class="staged-group-matches-list">
              @for (device of matchedDevices(); track device.id) {
                <li class="staged-group-match">
                  <span class="match-name">{{ device.name }}</span>
                  <span class="match-loc">{{ device.location }}</span>
                </li>
              }
            </ul>
          }
        </div>
      </div>
    </article>
  `,
  styleUrl: './staged-group-card.scss',
})
export class StagedGroupCard implements OnInit {
  protected readonly DeviceType = DeviceType;
  protected readonly ANY_LOCATION_VALUE = ANY_LOCATION_VALUE;

  /* ─────────────── Inputs / outputs ─────────────── */

  readonly group = input.required<StagedGroupTarget>();
  readonly allDevices = input.required<readonly AnyDevice[]>();
  readonly prefilledActions = input<readonly SceneActionRequest[] | null>(null);

  readonly remove = output<void>();

  /**
   * Emitted when the user changes the location scope. The parent
   * updates its stagedGroups array (the group's composite id changes
   * with location) and may detect duplicates.
   */
  readonly groupChanged = output<StagedGroupTarget>();

  /* ─────────────── Internal state ─────────────── */

  protected readonly stagedState = signal<StagedGroupState | null>(null);
  protected readonly touched = signal<ReadonlySet<TouchedProperty>>(new Set());
  protected readonly actionCount = computed(() => this.touched().size);
  protected readonly expanded = signal<boolean>(false);

  /* ─────────────── Derived display ─────────────── */

  /**
   * Devices currently matching this group's (deviceType, location).
   * Recomputes on either input change — including SSE-driven device
   * additions/removals via the parent's allDevices signal.
   */
  protected readonly matchedDevices = computed<readonly AnyDevice[]>(() => {
    const g = this.group();
    return this.allDevices().filter(d => {
      if (d.type !== g.deviceType) return false;
      if (g.location !== null && d.location !== g.location) return false;
      return true;
    });
  });

  /**
   * Locations that currently host at least one device of the group's
   * type, plus the synthetic "Any location" option. Used by the
   * location dropdown.
   */
  protected readonly locationOptions = computed<LocationOption[]>(() => {
    const type = this.group().deviceType;
    const locations = new Set<string>();
    for (const d of this.allDevices()) {
      if (d.type === type) locations.add(d.location);
    }
    const sorted = Array.from(locations).sort((a, b) => a.localeCompare(b));
    return [
      { label: 'Any location', value: ANY_LOCATION_VALUE },
      ...sorted.map(loc => ({ label: loc, value: loc })),
    ];
  });

  protected readonly selectedLocationValue = computed<string>(() => {
    const loc = this.group().location;
    return loc === null ? ANY_LOCATION_VALUE : loc;
  });

  /**
   * Display title — "All Lights" or "Lights in Bedroom".
   */
  protected readonly groupTitle = computed<string>(() => {
    const g = this.group();
    const typePlural = this.deviceTypePlural(g.deviceType);
    return g.location === null
      ? `All ${typePlural}`
      : `${typePlural} in ${g.location}`;
  });

  protected readonly groupMeta = computed<string>(() => {
    const g = this.group();
    const matchCount = this.matchedDevices().length;
    const matchWord = matchCount === 1 ? 'device matches' : 'devices match';
    const scope = g.location === null ? 'Any location' : g.location;
    return `${scope} · ${matchCount} ${matchWord}`;
  });

  protected readonly summary = computed<string>(() => {
    const s = this.stagedState();
    if (s === null) return '';

    switch (s.type) {
      case DeviceType.Light: {
        const power = s.powerState === PowerState.On ? 'On' : 'Off';
        if (s.powerState === PowerState.Off) return power;
        return `${power} · ${s.brightness}% · ${s.colorHex.toUpperCase()}`;
      }
      case DeviceType.Fan: {
        const power = s.powerState === PowerState.On ? 'On' : 'Off';
        if (s.powerState === PowerState.Off) return power;
        return `${power} · ${s.speed}`;
      }
      case DeviceType.Thermostat: {
        if (s.state === ThermostatState.Off) return 'Off';
        return `${s.mode} · ${s.desiredTemperature}°F`;
      }
      case DeviceType.DoorLock:
        return s.lockState === DoorLockState.Locked ? 'Locked' : 'Unlocked';
    }
  });

  /**
   * The first matched device, used as the visualization template's
   * stand-in for name/location and (for thermostats) ambient temp.
   * Null when no devices match — defaults take over via the *Name /
   * *Location helpers below.
   */
  protected readonly templateDevice = computed<AnyDevice | null>(() => {
    const matches = this.matchedDevices();
    if (matches.length === 0) return null;
    // Sort alphabetically for deterministic template selection — the
    // template shouldn't change just because devices arrived in a
    // different order via SSE.
    const sorted = [...matches].sort((a, b) => a.name.localeCompare(b.name));
    return sorted[0];
  });

  protected readonly templateName = computed<string>(() => {
    const t = this.templateDevice();
    if (t !== null) return t.name;
    return this.groupTitle();
  });

  protected readonly templateLocation = computed<string>(() => {
    const t = this.templateDevice();
    if (t !== null) return t.location;
    const g = this.group();
    return g.location === null ? 'Any location' : g.location;
  });

  protected readonly templateAmbient = computed<number>(() => {
    const t = this.templateDevice();
    if (t !== null && t.type === DeviceType.Thermostat) {
      return t.ambientTemperature;
    }
    return FALLBACK_DEFAULTS.thermostat.ambientTemperature;
  });

  /* ─────────────── Lifecycle ─────────────── */

  ngOnInit(): void {
    let state = this.buildInitialState();
    let touched: TouchedProperty[] = [];

    const actions = this.prefilledActions();
    if (actions !== null && actions.length > 0) {
      for (const action of actions) {
        const result = this.applyActionToState(state, action);
        if (result === null) continue;
        state = result.state;
        touched.push(result.touched);
      }
    }

    this.stagedState.set(state);
    if (touched.length > 0) {
      this.touched.set(new Set(touched));
    }
  }

  /* ─────────────── Event handlers ─────────────── */

  protected onToggleExpanded(): void {
    this.expanded.update(v => !v);
  }

  protected onLocationChange(value: string): void {
    const newLocation = value === ANY_LOCATION_VALUE ? null : value;
    const current = this.group();
    if (current.location === newLocation) return;
    this.groupChanged.emit(
      makeStagedGroupTarget(current.deviceType, newLocation),
    );
  }

  protected onLockStateChange(next: DoorLockState): void {
    this.stagedState.update(s =>
      s !== null && s.type === DeviceType.DoorLock
        ? { ...s, lockState: next }
        : s);
    this.markTouched('lockState');
  }

  protected onLightPowerChange(next: PowerState): void {
    this.stagedState.update(s =>
      s !== null && s.type === DeviceType.Light
        ? { ...s, powerState: next }
        : s);
    this.markTouched('power');
  }

  protected onLightBrightnessChange(next: number): void {
    this.stagedState.update(s =>
      s !== null && s.type === DeviceType.Light
        ? { ...s, brightness: next }
        : s);
    this.markTouched('brightness');
  }

  protected onLightColorChange(next: string): void {
    this.stagedState.update(s =>
      s !== null && s.type === DeviceType.Light
        ? { ...s, colorHex: next }
        : s);
    this.markTouched('color');
  }

  protected onFanPowerChange(next: PowerState): void {
    this.stagedState.update(s =>
      s !== null && s.type === DeviceType.Fan
        ? { ...s, powerState: next }
        : s);
    this.markTouched('power');
  }

  protected onFanSpeedChange(next: FanSpeed): void {
    this.stagedState.update(s =>
      s !== null && s.type === DeviceType.Fan
        ? { ...s, speed: next }
        : s);
    this.markTouched('speed');
  }

  protected onThermostatStateChange(next: ThermostatState): void {
    this.stagedState.update(s =>
      s !== null && s.type === DeviceType.Thermostat
        ? { ...s, state: next }
        : s);
    this.markTouched('power');
  }

  protected onThermostatDesiredTempChange(next: number): void {
    this.stagedState.update(s =>
      s !== null && s.type === DeviceType.Thermostat
        ? { ...s, desiredTemperature: next }
        : s);
    this.markTouched('desiredTemperature');
  }

  protected onThermostatModeChange(next: ThermostatMode): void {
    this.stagedState.update(s =>
      s !== null && s.type === DeviceType.Thermostat
        ? { ...s, mode: next }
        : s);
    this.markTouched('mode');
  }

  private markTouched(prop: TouchedProperty): void {
    this.touched.update(t => {
      if (t.has(prop)) return t;
      const next = new Set(t);
      next.add(prop);
      return next;
    });
  }

  /* ─────────────── State construction ─────────────── */

  /**
   * Builds the initial group state. Prefers the first matched device
   * as a template (its values become the starting point); falls back
   * to safe defaults if the group resolves to zero matches.
   */
  private buildInitialState(): StagedGroupState {
    const t = this.templateDevice();
    const type = this.group().deviceType;

    switch (type) {
      case DeviceType.Light:
        if (t !== null && t.type === DeviceType.Light) {
          return {
            type: DeviceType.Light,
            powerState: t.powerState,
            brightness: t.brightness,
            colorHex: t.colorHex,
          };
        }
        return {
          type: DeviceType.Light,
          ...FALLBACK_DEFAULTS.light,
        };

      case DeviceType.Fan:
        if (t !== null && t.type === DeviceType.Fan) {
          return {
            type: DeviceType.Fan,
            powerState: t.powerState,
            speed: t.speed,
          };
        }
        return {
          type: DeviceType.Fan,
          ...FALLBACK_DEFAULTS.fan,
        };

      case DeviceType.Thermostat:
        if (t !== null && t.type === DeviceType.Thermostat) {
          return {
            type: DeviceType.Thermostat,
            state: t.state,
            mode: t.mode,
            desiredTemperature: t.desiredTemperature,
          };
        }
        return {
          type: DeviceType.Thermostat,
          state: FALLBACK_DEFAULTS.thermostat.state,
          mode: FALLBACK_DEFAULTS.thermostat.mode,
          desiredTemperature: FALLBACK_DEFAULTS.thermostat.desiredTemperature,
        };

      case DeviceType.DoorLock:
        if (t !== null && t.type === DeviceType.DoorLock) {
          return {
            type: DeviceType.DoorLock,
            lockState: t.lockState,
          };
        }
        return {
          type: DeviceType.DoorLock,
          ...FALLBACK_DEFAULTS.doorLock,
        };
    }
  }

  /**
   * Folds a single SceneAction into a staged state, returning the new
   * state and the property key to mark as touched. Identical contract
   * to StagedTargetCard's applyActionToState; differs only in operating
   * on a group state shape rather than a device state shape.
   */
  private applyActionToState(
    state: StagedGroupState,
    action: SceneActionRequest,
  ): { state: StagedGroupState; touched: TouchedProperty } | null {
    const value = action.value;

    switch (action.operation) {
      case 'SetPower': {
        if (state.type === DeviceType.Light || state.type === DeviceType.Fan) {
          return {
            state: {
              ...state,
              powerState: value === 'On' ? PowerState.On : PowerState.Off,
            },
            touched: 'power',
          };
        }
        if (state.type === DeviceType.Thermostat) {
          return {
            state: value === 'Off'
              ? { ...state, state: ThermostatState.Off }
              : state,
            touched: 'power',
          };
        }
        return null;
      }

      case 'SetBrightness': {
        if (state.type !== DeviceType.Light) return null;
        const n = Number(value);
        if (Number.isNaN(n)) return null;
        return {
          state: { ...state, brightness: n },
          touched: 'brightness',
        };
      }

      case 'SetColor': {
        if (state.type !== DeviceType.Light) return null;
        if (typeof value !== 'string') return null;
        return {
          state: { ...state, colorHex: value },
          touched: 'color',
        };
      }

      case 'SetSpeed': {
        if (state.type !== DeviceType.Fan) return null;
        if (typeof value !== 'string') return null;
        return {
          state: { ...state, speed: value as FanSpeed },
          touched: 'speed',
        };
      }

      case 'SetMode': {
        if (state.type !== DeviceType.Thermostat) return null;
        if (typeof value !== 'string') return null;
        return {
          state: { ...state, mode: value as ThermostatMode },
          touched: 'mode',
        };
      }

      case 'SetDesiredTemperature': {
        if (state.type !== DeviceType.Thermostat) return null;
        const n = Number(value);
        if (Number.isNaN(n)) return null;
        return {
          state: { ...state, desiredTemperature: n },
          touched: 'desiredTemperature',
        };
      }

      case 'Lock': {
        if (state.type !== DeviceType.DoorLock) return null;
        return {
          state: { ...state, lockState: DoorLockState.Locked },
          touched: 'lockState',
        };
      }

      case 'Unlock': {
        if (state.type !== DeviceType.DoorLock) return null;
        return {
          state: { ...state, lockState: DoorLockState.Unlocked },
          touched: 'lockState',
        };
      }

      default:
        return null;
    }
  }

  /* ─────────────── Display helpers ─────────────── */

  private deviceTypePlural(type: DeviceType): string {
    switch (type) {
      case DeviceType.Light: return 'Lights';
      case DeviceType.Fan: return 'Fans';
      case DeviceType.Thermostat: return 'Thermostats';
      case DeviceType.DoorLock: return 'Door Locks';
    }
  }

  /* ─────────────── Save (called by parent dialog) ─────────────── */

  /**
   * Walks the touched set and emits one SceneActionRequest per touched
   * property. Each action carries deviceType + location (no deviceId)
   * so the backend resolves the group at execute time.
   *
   * Order within the array is not significant — the server's
   * SceneActionNormalizer applies the priority table.
   */
  toActions(): SceneActionRequest[] {
    const actions: SceneActionRequest[] = [];
    const s = this.stagedState();
    if (s === null) return actions;

    const touched = this.touched();
    const g = this.group();
    const deviceType = g.deviceType;
    const location = g.location;

    switch (s.type) {
      case DeviceType.DoorLock:
        if (touched.has('lockState')) {
          actions.push({
            deviceId: null,
            deviceType,
            location,
            operation: s.lockState === DoorLockState.Locked ? 'Lock' : 'Unlock',
            value: null,
          });
        }
        break;

      case DeviceType.Light:
        if (touched.has('power')) {
          actions.push({
            deviceId: null,
            deviceType,
            location,
            operation: 'SetPower',
            value: s.powerState === PowerState.On ? 'On' : 'Off',
          });
        }
        if (touched.has('brightness')) {
          actions.push({
            deviceId: null,
            deviceType,
            location,
            operation: 'SetBrightness',
            value: String(s.brightness),
          });
        }
        if (touched.has('color')) {
          actions.push({
            deviceId: null,
            deviceType,
            location,
            operation: 'SetColor',
            value: s.colorHex,
          });
        }
        break;

      case DeviceType.Fan:
        if (touched.has('power')) {
          actions.push({
            deviceId: null,
            deviceType,
            location,
            operation: 'SetPower',
            value: s.powerState === PowerState.On ? 'On' : 'Off',
          });
        }
        if (touched.has('speed')) {
          actions.push({
            deviceId: null,
            deviceType,
            location,
            operation: 'SetSpeed',
            value: s.speed,
          });
        }
        break;

      case DeviceType.Thermostat:
        if (touched.has('power')) {
          actions.push({
            deviceId: null,
            deviceType,
            location,
            operation: 'SetPower',
            value: s.state === ThermostatState.Off ? 'Off' : 'On',
          });
        }
        if (touched.has('mode')) {
          actions.push({
            deviceId: null,
            deviceType,
            location,
            operation: 'SetMode',
            value: s.mode,
          });
        }
        if (touched.has('desiredTemperature')) {
          actions.push({
            deviceId: null,
            deviceType,
            location,
            operation: 'SetDesiredTemperature',
            value: String(s.desiredTemperature),
          });
        }
        break;
    }

    return actions;
  }
}
