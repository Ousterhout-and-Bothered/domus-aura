import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  input,
  output,
  signal,
} from '@angular/core';

import { DeviceType, PowerState } from '../../../device/models/device';
import {
  AnyDevice,
  DoorLockState,
  FanSpeed,
  ThermostatMode,
  ThermostatState,
} from '../../../device/models/device-types';
import { SceneActionRequest } from '../../models/scene';

// Leaf visuals — reused from the dashboard, same shapes the existing
// device-card binds to.
import { LightBulb } from '../../../device/components/light-bulb/light-bulb';
import { FanSpinning } from '../../../device/components/fan-spinning/fan-spinning';
import { ThermostatGauge } from '../../../device/components/thermostat-gauge/thermostat-gauge';
import { DoorLock } from '../../../device/components/door-lock/door-lock';

/**
 * Local discriminated-union of the staged values for a target.
 * Discriminator matches the device's runtime type and is fixed at
 * snapshot time — never reassigned over a card's lifetime.
 */
type StagedDeviceState =
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

/**
 * Property keys that can be marked as user-touched for a staged target.
 * On Save, the dialog asks each card for toActions(), which walks this
 * set to emit one SceneActionRequest per touched property.
 */
type TouchedProperty =
  | 'power'
  | 'brightness'
  | 'color'
  | 'speed'
  | 'mode'
  | 'desiredTemperature'
  | 'lockState';

/**
 * One staged target inside the scene editor dialog. Wraps an existing
 * dashboard leaf visual and tracks the user's edits as a discriminated
 * staged state plus a touched-properties set.
 *
 * Two modes:
 *   - Create mode: card initialises from the device snapshot only;
 *     `touched` starts empty.
 *   - Edit mode: card additionally accepts `prefilledActions` from an
 *     existing scene; for each action it overrides the matching staged
 *     value and marks the property as touched. The user's subsequent
 *     edits work identically.
 *
 * The device input is treated as a snapshot, captured once in
 * ngOnInit. The staged signal has no reactive dependency on the input,
 * so upstream SSE-driven updates to the device list cannot reset the
 * user's in-progress edits.
 */
@Component({
  selector: 'aura-staged-target-card',
  standalone: true,
  imports: [LightBulb, FanSpinning, ThermostatGauge, DoorLock],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="staged-target" [class.is-expanded]="expanded()">
      <header class="staged-target-head">
        <div class="staged-target-titles">
          <h3 class="staged-target-name">{{ device().name }}</h3>
          <p class="staged-target-loc">{{ device().location }}</p>
        </div>
        <span class="staged-target-badge">{{ actionCount() }} actions</span>
        <button
          type="button"
          class="staged-target-remove"
          [attr.aria-label]="'Remove ' + device().name + ' from scene'"
          (click)="remove.emit()"
        >
          <i class="pi pi-times"></i>
        </button>
      </header>

      <div class="staged-target-summary">
        <p class="summary-text">{{ summary() }}</p>
        <button
          type="button"
          class="staged-target-configure"
          (click)="onToggleExpanded()"
        >
          <i class="pi pi-pencil"></i>
          Configure
        </button>
      </div>

      <div class="staged-target-body">
        <div class="staged-target-body-actions">
          <button
            type="button"
            class="staged-target-done"
            (click)="onToggleExpanded()"
          >
            <i class="pi pi-check"></i>
            Done
          </button>
        </div>

        @switch (device().type) {
          @case (DeviceType.DoorLock) {
            @let s = stagedState();
            @if (s !== null && s.type === DeviceType.DoorLock) {
              <aura-door-lock
                [name]="device().name"
                [location]="device().location"
                [lockState]="s.lockState"
                (lockStateChange)="onLockStateChange($event)"
              />
            }
          }

          @case (DeviceType.Light) {
            @let s = stagedState();
            @if (s !== null && s.type === DeviceType.Light) {
              <aura-light-bulb
                [name]="device().name"
                [location]="device().location"
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
                [name]="device().name"
                [location]="device().location"
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
                [name]="device().name"
                [location]="device().location"
                [desiredTemperature]="s.desiredTemperature"
                [ambientTemperature]="getAmbientTemperature()"
                [mode]="s.mode"
                [state]="s.state"
                (stateChange)="onThermostatStateChange($event)"
                (desiredTemperatureChange)="onThermostatDesiredTempChange($event)"
                (modeChange)="onThermostatModeChange($event)"
              />
            }
          }
        }
      </div>
    </article>
  `,
  styleUrl: './staged-target-card.scss',
})
export class StagedTargetCard implements OnInit {
  // Expose DeviceType to the template's @switch / @case expressions.
  protected readonly DeviceType = DeviceType;

  readonly device = input.required<AnyDevice>();

  /**
   * Optional pre-existing scene actions for this device. When present,
   * the card initialises in "edit mode": each action overrides its
   * matching property in stagedState and marks the property as touched.
   * Absent in create mode.
   */
  readonly prefilledActions = input<readonly SceneActionRequest[] | null>(null);

  readonly remove = output<void>();

  protected readonly stagedState = signal<StagedDeviceState | null>(null);
  protected readonly touched = signal<ReadonlySet<TouchedProperty>>(new Set());
  protected readonly actionCount = computed(() => this.touched().size);
  protected readonly expanded = signal<boolean>(false);

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

  protected onToggleExpanded(): void {
    this.expanded.update(v => !v);
  }

  ngOnInit(): void {
    // Always start from the device snapshot — this provides defaults
    // for properties the scene actions don't touch.
    let state = this.buildInitialState(this.device());
    let touched: TouchedProperty[] = [];

    // If editing an existing scene, fold each action into the state
    // and record it as touched.
    const actions = this.prefilledActions();
    if (actions !== null && actions.length > 0) {
      for (const action of actions) {
        const result = this.applyActionToState(state, action);
        if (result === null) continue; // unknown op — skip silently
        state = result.state;
        touched.push(result.touched);
      }
    }

    this.stagedState.set(state);
    if (touched.length > 0) {
      this.touched.set(new Set(touched));
    }
  }

  /* ─────────────── Change handlers ─────────────── */

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

  protected getAmbientTemperature(): number {
    const d = this.device();
    return d.type === DeviceType.Thermostat ? d.ambientTemperature : 0;
  }

  private buildInitialState(d: AnyDevice): StagedDeviceState {
    switch (d.type) {
      case DeviceType.Light:
        return {
          type: DeviceType.Light,
          powerState: d.powerState,
          brightness: d.brightness,
          colorHex: d.colorHex,
        };
      case DeviceType.Fan:
        return {
          type: DeviceType.Fan,
          powerState: d.powerState,
          speed: d.speed,
        };
      case DeviceType.Thermostat:
        return {
          type: DeviceType.Thermostat,
          state: d.state,
          mode: d.mode,
          desiredTemperature: d.desiredTemperature,
        };
      case DeviceType.DoorLock:
        return {
          type: DeviceType.DoorLock,
          lockState: d.lockState,
        };
    }
  }

  /**
   * Folds a single SceneAction into a staged state, returning the new
   * state and the property key to mark as touched. Returns null if the
   * action's operation does not match the device's type — defensive,
   * since scene actions and device types should be paired correctly,
   * but a stale scene targeting a now-different device shouldn't crash
   * the editor.
   */
  private applyActionToState(
    state: StagedDeviceState,
    action: SceneActionRequest,
  ): { state: StagedDeviceState; touched: TouchedProperty } | null {
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
          // "Off" sets state to Off; "On" preserves the device's current
          // non-Off state (set by buildInitialState from the snapshot).
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

  /**
   * Walks the touched set and emits one SceneActionRequest per touched
   * property. Order within the array is not significant — the server's
   * SceneActionNormalizer applies the priority table.
   */
  toActions(): SceneActionRequest[] {
    const actions: SceneActionRequest[] = [];
    const s = this.stagedState();
    if (s === null) return actions;

    const touched = this.touched();
    const deviceId = this.device().id;

    switch (s.type) {
      case DeviceType.DoorLock:
        if (touched.has('lockState')) {
          actions.push({
            deviceId,
            operation: s.lockState === DoorLockState.Locked ? 'Lock' : 'Unlock',
            value: null,
          });
        }
        break;

      case DeviceType.Light:
        if (touched.has('power')) {
          actions.push({
            deviceId,
            operation: 'SetPower',
            value: s.powerState === PowerState.On ? 'On' : 'Off',
          });
        }
        if (touched.has('brightness')) {
          actions.push({
            deviceId,
            operation: 'SetBrightness',
            value: String(s.brightness),
          });
        }
        if (touched.has('color')) {
          actions.push({
            deviceId,
            operation: 'SetColor',
            value: s.colorHex,
          });
        }
        break;

      case DeviceType.Fan:
        if (touched.has('power')) {
          actions.push({
            deviceId,
            operation: 'SetPower',
            value: s.powerState === PowerState.On ? 'On' : 'Off',
          });
        }
        if (touched.has('speed')) {
          actions.push({
            deviceId,
            operation: 'SetSpeed',
            value: s.speed,
          });
        }
        break;

      case DeviceType.Thermostat:
        if (touched.has('power')) {
          actions.push({
            deviceId,
            operation: 'SetPower',
            value: s.state === ThermostatState.Off ? 'Off' : 'On',
          });
        }
        if (touched.has('mode')) {
          actions.push({
            deviceId,
            operation: 'SetMode',
            value: s.mode,
          });
        }
        if (touched.has('desiredTemperature')) {
          actions.push({
            deviceId,
            operation: 'SetDesiredTemperature',
            value: String(s.desiredTemperature),
          });
        }
        break;
    }

    return actions;
  }
}
