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
 *
 * Note: 'power' is the canonical name for any toggle that maps to a
 * SetPower action — for thermostats this is bound to the gauge's state
 * change event, even though the staged shape carries `state` rather
 * than `powerState`. The touched-property name reflects the emitted
 * action, not the leaf-visual binding.
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
 * The device input is treated as a snapshot, captured once in
 * ngOnInit. The staged signal has no reactive dependency on the input,
 * so upstream SSE-driven updates to the device list cannot reset the
 * user's in-progress edits. The parent dialog is also responsible for
 * keeping the same device reference bound (it stores AnyDevice
 * snapshots, not ids) — together these guarantee stable card state
 * across the dialog's lifetime.
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

      <!-- Collapsed summary: shown when not expanded -->
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

      <!-- Expanded body: always in DOM (kept alive for state),
           CSS-hidden when collapsed -->
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
  readonly remove = output<void>();

  /**
   * Staged values for this target. Null until ngOnInit snapshots the
   * device input. After that point, only the user's change handlers
   * mutate it — upstream input changes (from SSE updates) never
   * reset it because the signal has no reactive dependencies.
   */
  protected readonly stagedState = signal<StagedDeviceState | null>(null);

  /**
   * Set of property keys the user has explicitly modified. A property
   * counts as touched the moment its handler fires, even if the value
   * matches the original. This is deliberate: an explicit choice to
   * "set brightness to its current value" is still a scene action.
   */
  protected readonly touched = signal<ReadonlySet<TouchedProperty>>(new Set());

  /**
   * Whether this card's full configuration UI is visible. Default false:
   * cards start collapsed so users can scan a long list of staged
   * devices without scrolling through the full visualization for each.
   */
  protected readonly expanded = signal<boolean>(false);

  // Drives the "N actions" badge in the header.
  protected readonly actionCount = computed(() => this.touched().size);

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

  ngOnInit(): void {
    console.log('[card] ngOnInit for', this.device().name, 'id:', this.device().id);
    this.stagedState.set(this.buildInitialState(this.device()));
  }

  /* ─────────────── Change handlers ─────────────── */

  /**
   * Toggle handler for the Configure / Done button.
   */
  protected onToggleExpanded(): void {
    this.expanded.update(v => !v);
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
    console.log('[card] onFanSpeedChange for', this.device().name, 'next:', next, 'before:', this.stagedState());
    this.stagedState.update(s =>
      s !== null && s.type === DeviceType.Fan
        ? { ...s, speed: next }
        : s);
    console.log('[card] onFanSpeedChange after:', this.stagedState());
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

  /**
   * Idempotent touch marker. If the property is already touched, returns
   * the same set without allocating a new one — this avoids spurious
   * re-renders when the user wiggles a control that's already touched.
   */
  private markTouched(prop: TouchedProperty): void {
    this.touched.update(t => {
      if (t.has(prop)) return t;
      const next = new Set(t);
      next.add(prop);
      return next;
    });
  }

  /**
   * Returns the live ambient temperature for thermostat targets.
   * Ambient is environmental — not a stageable property — so it's read
   * straight from the device input rather than the staged signal.
   */
  protected getAmbientTemperature(): number {
    const d = this.device();
    return d.type === DeviceType.Thermostat ? d.ambientTemperature : 0;
  }

  /**
   * Builds the initial staged state from the device snapshot. Called
   * once from ngOnInit.
   */
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
   * Walks the touched set and emits one SceneActionRequest per touched
   * property, using the canonical backend operation strings:
   * SetPower, SetBrightness, SetColor, SetSpeed, SetMode,
   * SetDesiredTemperature, Lock, Unlock.
   *
   * Called by the parent dialog on Save. Order within the returned
   * array is not significant — SceneActionNormalizer (server-side)
   * applies the priority table.
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
