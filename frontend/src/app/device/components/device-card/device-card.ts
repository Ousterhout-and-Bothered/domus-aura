import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
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

@Component({
  selector: 'aura-device-card',
  standalone: true,
  imports: [ThermostatGauge, FanSpinning, LightBulb, DoorLock],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
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
            (desiredTemperatureChange)="onSetDesiredTemperature(t.id, $event)"
            (modeChange)="onSetMode(t.id, $event)"
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
            (powerStateChange)="onSetLightPower(l.id, $event)"
            (brightnessChange)="onSetLightBrightness(l.id, $event)"
            (colorHexChange)="onSetLightColor(l.id, $event)"
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
            (powerStateChange)="onSetFanPower(f.id, $event)"
            (speedChange)="onSetFanSpeed(f.id, $event)"
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
            (lockStateChange)="onSetLockState(dl.id, $event)"
          />
        }
      }
    }
  `,
  styleUrl: './device-card.scss',
})
export class DeviceCard {
  private readonly deviceApi = inject(DeviceApiService);

  readonly device = input.required<AnyDevice>();

  /** Emitted when the device state changes (after a command succeeds). */
  readonly deviceUpdated = output<AnyDevice>();

  readonly DeviceType = DeviceType;
  readonly isThermostat = isThermostat;
  readonly isFan = isFan;
  readonly isLight = isLight;
  readonly isDoorLock = isDoorLock;

  asThermostat(d: AnyDevice): Thermostat {
    return d as Thermostat;
  }

  asFan(d: AnyDevice): Fan {
    return d as Fan;
  }

  asLight(d: AnyDevice): Light {
    return d as Light;
  }

  asDoorLock(d: AnyDevice): DoorLockDevice {
    return d as DoorLockDevice;
  }

  onSetDesiredTemperature(deviceId: string, value: number): void {
    this.deviceApi.executeCommand(deviceId, {
      command: 'setDesiredTemperature',
      value,
    }).subscribe({
      next: (updated) => this.deviceUpdated.emit(updated),
      error: (err) => console.error('Failed to set temperature', err),
    });
  }

  onSetMode(deviceId: string, mode: ThermostatMode): void {
    this.deviceApi.executeCommand(deviceId, {
      command: 'setMode',
      value: mode,
    }).subscribe({
      next: (updated) => this.deviceUpdated.emit(updated),
      error: (err) => console.error('Failed to set mode', err),
    });
  }

  onSetFanPower(deviceId: string, powerState: PowerState): void {
    this.deviceApi.executeCommand(deviceId, {
      command: 'setPower',
      value: powerState,
    }).subscribe({
      next: (updated) => this.deviceUpdated.emit(updated),
      error: (err) => console.error('Failed to set fan power', err),
    });
  }

  onSetFanSpeed(deviceId: string, speed: FanSpeed): void {
    this.deviceApi.executeCommand(deviceId, {
      command: 'setSpeed',
      value: speed,
    }).subscribe({
      next: (updated) => this.deviceUpdated.emit(updated),
      error: (err) => console.error('Failed to set fan speed', err),
    });
  }

  onSetLightPower(deviceId: string, powerState: PowerState): void {
    this.deviceApi.executeCommand(deviceId, {
      command: 'setPower',
      value: powerState,
    }).subscribe({
      next: (updated) => this.deviceUpdated.emit(updated),
      error: (err) => console.error('Failed to set light power', err),
    });
  }

  onSetLightBrightness(deviceId: string, brightness: number): void {
    this.deviceApi.executeCommand(deviceId, {
      command: 'setBrightness',
      value: brightness,
    }).subscribe({
      next: (updated) => this.deviceUpdated.emit(updated),
      error: (err) => console.error('Failed to set brightness', err),
    });
  }

  onSetLightColor(deviceId: string, colorHex: string): void {
    this.deviceApi.executeCommand(deviceId, {
      command: 'setColor',
      value: colorHex,
    }).subscribe({
      next: (updated) => this.deviceUpdated.emit(updated),
      error: (err) => console.error('Failed to set color', err),
    });
  }

  onSetLockState(deviceId: string, state: DoorLockState): void {
    const command = state === 'Locked' ? 'lock' : 'unlock';
    this.deviceApi.executeCommand(deviceId, {
      command,
    }).subscribe({
      next: (updated) => this.deviceUpdated.emit(updated),
      error: (err) => console.error('Failed to set lock state', err),
    });
  }
}
