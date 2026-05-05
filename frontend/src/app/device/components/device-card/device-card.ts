import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';

import { DeviceType } from '../../models/device';
import {
  AnyDevice,
  DoorLock as DoorLockDevice,
  DoorLockState,
  Fan,
  FanSpeed,
  Light,
  Thermostat,
  ThermostatMode,
  ThermostatState,
  isDoorLock,
  isFan,
  isLight,
  isThermostat
} from '../../models/device-types';
import { PowerState } from '../../models/device';
import { DeviceApiService } from '../../services/device-api.service';
import { ThermostatGauge } from '../thermostat-gauge/thermostat-gauge';
import { FanSpinning } from '../fan-spinning/fan-spinning';
import { LightBulb } from '../light-bulb/light-bulb';
import { DoorLock } from '../door-lock/door-lock';

/**
 * Container component for a single smart home device.
 *
 * It dynamically renders the appropriate specialized component (e.g.,
 * `ThermostatGauge`, `LightBulb`) based on the device type and handles
 * command execution and device removal requests.
 */
@Component({
  selector: 'aura-device-card',
  standalone: true,
  imports: [ThermostatGauge, FanSpinning, LightBulb, DoorLock, ButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="device-card-host">
      <button
        type="button"
        class="device-card-remove"
        [attr.aria-label]="'Remove ' + device().name"
        (click)="onRequestRemove($event)"
      >
        <i class="pi pi-times"></i>
      </button>

      @switch (device().type) {
        @case (DeviceType.Thermostat) {
          @if (isThermostat(device())) {
            @let t = asThermostat(device());
            <aura-thermostat-gauge
              [name]="t.name"
              [location]="t.location"
              [desiredTemperature]="t.desiredTemperature"
              [ambientTemperature]="t.ambientTemperature"
              [mode]="t.mode"
              [state]="t.state"
              (stateChange)="onSetThermostatState(t, $event)"
              (desiredTemperatureChange)="onSetDesiredTemperature(t, $event)"
              (modeChange)="onSetMode(t, $event)"
            />
          }
        }
        @case (DeviceType.Light) {
          @if (isLight(device())) {
            @let l = asLight(device());
            <aura-light-bulb
              [name]="l.name"
              [location]="l.location"
              [powerState]="l.powerState"
              [brightness]="l.brightness"
              [colorHex]="l.colorHex"
              (powerStateChange)="onSetLightPower(l, $event)"
              (brightnessChange)="onSetLightBrightness(l, $event)"
              (colorHexChange)="onSetLightColor(l, $event)"
            />
          }
        }
        @case (DeviceType.Fan) {
          @if (isFan(device())) {
            @let f = asFan(device());
            <aura-fan-spinning
              [name]="f.name"
              [location]="f.location"
              [powerState]="f.powerState"
              [speed]="f.speed"
              (powerStateChange)="onSetFanPower(f, $event)"
              (speedChange)="onSetFanSpeed(f, $event)"
            />
          }
        }

        @case (DeviceType.DoorLock) {
          @if (isDoorLock(device())) {
            @let dl = asDoorLock(device());
            <aura-door-lock
              [name]="dl.name"
              [location]="dl.location"
              [lockState]="dl.lockState"
              (lockStateChange)="onSetLockState(dl, $event)"
            />
          }
        }
      }
    </div>
  `,
  styleUrl: './device-card.scss',
})
export class DeviceCard {
  private readonly deviceApi = inject(DeviceApiService);
  private readonly messages = inject(MessageService);
  private readonly confirms = inject(ConfirmationService);

  /** The device object to display and manage. */
  readonly device = input.required<AnyDevice>();

  /** Emits the updated device object after a state change. */
  readonly deviceUpdated = output<AnyDevice>();

  /** Emits the unique identifier of the device when it is successfully removed. */
  readonly deviceRemoved = output<string>();

  readonly DeviceType = DeviceType;
  readonly isThermostat = isThermostat;
  readonly isFan = isFan;
  readonly isLight = isLight;
  readonly isDoorLock = isDoorLock;

  asThermostat(d: AnyDevice): Thermostat { return d as Thermostat; }
  asFan(d: AnyDevice): Fan { return d as Fan; }
  asLight(d: AnyDevice): Light { return d as Light; }
  asDoorLock(d: AnyDevice): DoorLockDevice { return d as DoorLockDevice; }

  /* ─────────────── Thermostat ─────────────── */

  onSetThermostatState(thermostat: Thermostat, state: ThermostatState): void {
    this.deviceApi.executeCommand(thermostat.id, {
      command: 'setPower',
      value: state === ThermostatState.Off ? 'Off' : 'On',
    }).subscribe({
      next: (updated) => {
        this.deviceUpdated.emit(updated);
        this.messages.add({
          severity: 'success',
          summary: thermostat.name,
          detail: state === ThermostatState.Off ? 'Turned off' : 'Turned on',
          life: 2500,
        });
      },
      error: (err) => this.commandFailed(thermostat.name, 'state', err),
    });
  }

  onSetDesiredTemperature(thermostat: Thermostat, value: number): void {
    this.deviceApi.executeCommand(thermostat.id, {
      command: 'setDesiredTemperature',
      value,
    }).subscribe({
      next: (updated) => {
        this.deviceUpdated.emit(updated);
      },
      error: (err) => this.commandFailed(thermostat.name, 'temperature', err),
    });
  }

  onSetMode(thermostat: Thermostat, mode: ThermostatMode): void {
    this.deviceApi.executeCommand(thermostat.id, {
      command: 'setMode',
      value: mode,
    }).subscribe({
      next: (updated) => {
        this.deviceUpdated.emit(updated);
        this.messages.add({
          severity: 'success',
          summary: thermostat.name,
          detail: `Mode ${mode}`,
          life: 2000,
        });
      },
      error: (err) => this.commandFailed(thermostat.name, 'mode', err),
    });
  }

  /* ─────────────── Fan ─────────────── */

  onSetFanPower(fan: Fan, powerState: PowerState): void {
    this.deviceApi.executeCommand(fan.id, {
      command: 'setPower',
      value: powerState,
    }).subscribe({
      next: (updated) => {
        this.deviceUpdated.emit(updated);
        this.messages.add({
          severity: 'success',
          summary: fan.name,
          detail: powerState === PowerState.On ? 'Turned on' : 'Turned off',
          life: 2500,
        });
      },
      error: (err) => this.commandFailed(fan.name, 'power', err),
    });
  }

  onSetFanSpeed(fan: Fan, speed: FanSpeed): void {
    this.deviceApi.executeCommand(fan.id, {
      command: 'setSpeed',
      value: speed,
    }).subscribe({
      next: (updated) => {
        this.deviceUpdated.emit(updated);
        this.messages.add({
          severity: 'success',
          summary: fan.name,
          detail: `Speed ${speed}`,
          life: 2000,
        });
      },
      error: (err) => this.commandFailed(fan.name, 'speed', err),
    });
  }

  /* ─────────────── Light ───────────────*/

  onSetLightPower(light: Light, powerState: PowerState): void {
    this.deviceApi.executeCommand(light.id, {
      command: 'setPower',
      value: powerState,
    }).subscribe({
      next: (updated) => {
        this.deviceUpdated.emit(updated);
        this.messages.add({
          severity: 'success',
          summary: light.name,
          detail: powerState === PowerState.On ? 'Turned on' : 'Turned off',
          life: 2500,
        });
      },
      error: (err) => this.commandFailed(light.name, 'power', err),
    });
  }

  onSetLightBrightness(light: Light, brightness: number): void {
    this.deviceApi.executeCommand(light.id, {
      command: 'setBrightness',
      value: brightness,
    }).subscribe({
      next: (updated) => {
        this.deviceUpdated.emit(updated);
        this.messages.add({
          severity: 'success',
          summary: light.name,
          detail: `Brightness ${brightness}%`,
          life: 2000,
        });
      },
      error: (err) => this.commandFailed(light.name, 'brightness', err),
    });
  }

  onSetLightColor(light: Light, colorHex: string): void {
    this.deviceApi.executeCommand(light.id, {
      command: 'setColor',
      value: colorHex,
    }).subscribe({
      next: (updated) => {
        this.deviceUpdated.emit(updated);
        this.messages.add({
          severity: 'success',
          summary: light.name,
          detail: `Color ${colorHex.toUpperCase()}`,
          life: 2000,
        });
      },
      error: (err) => this.commandFailed(light.name, 'color', err),
    });
  }

  /* ─────────────── Door Lock (toasts wired) ─────────────── */

  onSetLockState(lock: DoorLockDevice, state: DoorLockState): void {
    const command = state === DoorLockState.Locked ? 'lock' : 'unlock';
    this.deviceApi.executeCommand(lock.id, {
      command,
    }).subscribe({
      next: (updated) => {
        this.deviceUpdated.emit(updated);
        this.messages.add({
          severity: 'success',
          summary: lock.name,
          detail: state === DoorLockState.Locked ? 'Locked' : 'Unlocked',
          life: 2500,
        });
      },
      error: (err) => this.commandFailed(lock.name, 'lock state', err),
    });
  }

  /* ─────────────── Removal ─────────────── */

  onRequestRemove(event: MouseEvent): void {
    // target the confirm popup at the × button so the popover anchors visibly
    this.confirms.confirm({
      target: event.currentTarget as HTMLElement,
      header: 'Remove device',
      message: `Remove "${this.device().name}"? This cannot be undone.`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Remove',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => this.executeRemove(),
    });
  }

  private executeRemove(): void {
    const device = this.device();
    this.deviceApi.remove(device.id).subscribe({
      next: () => {
        this.deviceRemoved.emit(device.id);
        this.messages.add({
          severity: 'success',
          summary: device.name,
          detail: 'Removed',
          life: 2500,
        });
      },
      error: (err) => {
        console.error(`Failed to remove ${device.name}`, err);
        this.messages.add({
          severity: 'error',
          summary: device.name,
          detail: 'Could not remove. Please try again.',
          life: 4000,
        });
      },
    });
  }

  /* ─────────────── Shared error toast ───────────────
   *
   * Temporary home for the error path. When we lift this to an HTTP
   * interceptor (the Lecture 16 pattern), this method goes away and
   * every command's error callback collapses to console.error or
   * just nothing.
   */
  private commandFailed(deviceName: string, action: string, err: unknown): void {
    console.error(`Failed to set ${action} on ${deviceName}`, err);
    this.messages.add({
      severity: 'error',
      summary: deviceName,
      detail: `Could not update ${action}. Please try again.`,
      life: 4000,
    });
  }
}
