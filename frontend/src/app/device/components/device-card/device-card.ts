import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { ConfirmationService, MessageService} from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { Router } from '@angular/router';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { EditDeviceDialog } from '../edit-device-dialog/edit-device-dialog';

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

@Component({
  selector: 'aura-device-card',
  standalone: true,
  imports: [ThermostatGauge, FanSpinning, LightBulb, DoorLock, ButtonModule, MenuModule, EditDeviceDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="device-card-host">
      <button
        type="button"
        class="device-card-kebab"
        [attr.aria-label]="'Actions for ' + device().name"
        (click)="kebabMenu.toggle($event)"
      >
        <i class="pi pi-ellipsis-v"></i>
      </button>

      <p-menu
        #kebabMenu
        [model]="menuItems()"
        [popup]="true"
        appendTo="body"
        styleClass="device-card-menu"
      />

      <aura-edit-device-dialog
        [visible]="editOpen()"
        (visibleChange)="editOpen.set($event)"
        [device]="device()"
        [existingLocations]="existingLocations()"
        (deviceUpdated)="onDeviceUpdated($event)"
      />

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
  readonly device = input.required<AnyDevice>();
  readonly existingLocations = input.required<string[]>();

  private readonly deviceApi = inject(DeviceApiService);
  private readonly messages = inject(MessageService);
  private readonly confirms = inject(ConfirmationService);
  private readonly router = inject(Router);

  readonly editOpen = signal(false);

  readonly menuItems = computed<MenuItem[]>(() => [
    {
      label: 'Edit',
      icon: 'pi pi-pencil',
      command: () => this.editOpen.set(true),
    },
    {
      label: 'View history',
      icon: 'pi pi-clock',
      command: () => this.onViewHistory(),
    },
    {
      separator: true,
    },
    {
      label: 'Delete',
      icon: 'pi pi-trash',
      styleClass: 'menu-item-danger',
      command: () => this.onRequestRemoveFromMenu(),
    },
  ]);

  readonly deviceUpdated = output<AnyDevice>();

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

  // Triggered from the kebab menus Delete item. The confirm popup anchors
  // at the host element since we don't have the original click target.
  onRequestRemoveFromMenu(): void {
    this.confirms.confirm({
      target: document.activeElement as HTMLElement,
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

  /* ─────────────── Edit ─────────────── */

  onDeviceUpdated(updated: AnyDevice): void {
    this.deviceUpdated.emit(updated);
  }

  /* ─────────────── View history ─────────────── */

  onViewHistory(): void {
    this.router.navigate(['/history'], {
      queryParams: { deviceId: this.device().id },
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
