import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { DeviceType } from '../../models/device';
import { AnyDevice, Thermostat, ThermostatMode, isThermostat } from '../../models/device-types';
import { DeviceApiService } from '../../services/device-api.service';
import { ThermostatGauge } from '../thermostat-gauge/thermostat-gauge';

@Component({
  selector: 'aura-device-card',
  standalone: true,
  imports: [ThermostatGauge],
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
        <div class="placeholder-card">
          <p class="placeholder-name">{{ device().name }}</p>
          <p class="placeholder-type">Light placeholder</p>
        </div>
      }
      @case (DeviceType.Fan) {
        <div class="placeholder-card">
          <p class="placeholder-name">{{ device().name }}</p>
          <p class="placeholder-type">Fan placeholder</p>
        </div>
      }
      @case (DeviceType.DoorLock) {
        <div class="placeholder-card">
          <p class="placeholder-name">{{ device().name }}</p>
          <p class="placeholder-type">Door lock placeholder</p>
        </div>
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

  asThermostat(d: AnyDevice): Thermostat {
    return d as Thermostat;
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
}
