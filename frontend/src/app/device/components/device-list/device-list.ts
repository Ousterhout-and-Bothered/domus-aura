import { ChangeDetectionStrategy, Component, computed, inject, signal, OnInit } from '@angular/core';
import { DeviceApiService } from '../../services/device-api.service';
import { AnyDevice } from '../../models/device-types';
import { RoomBlock } from '../room-block/room-block';

/**
 * The /devices route. Fetches every device on init, groups them by
 * location, and renders one RoomBlock per room.
 *
 * Live updates (SSE) are intentionally not wired yet. Once the static
 * render is solid, hook DeviceEventService into here and update the
 * `devices` signal on each event.
 */
@Component({
  selector: 'aura-device-list',
  standalone: true,
  imports: [RoomBlock],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="device-list">
      <header class="device-list-header">
        <h1>Your Home</h1>
        @if (devices().length > 0) {
          <p class="device-list-summary">
            {{ devices().length }} devices · {{ rooms().length }} rooms · {{ activeCount() }} active
          </p>
        }
      </header>

      @if (loading()) {
        <p class="device-list-status">Loading devices…</p>
      } @else if (error()) {
        <p class="device-list-status error">{{ error() }}</p>
      } @else {
        @for (room of rooms(); track room.location) {
          <aura-room-block
            [location]="room.location"
            [devices]="room.devices"
            (deviceUpdated)="onDeviceUpdated($event)"
          />
        }
      }
    </section>
  `,
  styleUrl: './device-list.scss',
})
export class DeviceList implements OnInit {
  private readonly deviceApi = inject(DeviceApiService);

  readonly devices = signal<AnyDevice[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  /** Devices grouped by location, sorted alphabetically by room name. */
  readonly rooms = computed(() => {
    const byLocation = new Map<string, AnyDevice[]>();
    for (const device of this.devices()) {
      const list = byLocation.get(device.location) ?? [];
      list.push(device);
      byLocation.set(device.location, list);
    }
    return Array.from(byLocation.entries())
      .map(([location, devices]) => ({ location, devices }))
      .sort((a, b) => a.location.localeCompare(b.location));
  });

  /** How many devices are currently active across the whole home. */
  readonly activeCount = computed(() =>
    this.devices().filter((d) => isDeviceActive(d)).length
  );

  /** Replace the matching device in the signal with the latest server snapshot. */
  onDeviceUpdated(updated: AnyDevice): void {
    this.devices.update((current) =>
      current.map((d) => (d.id === updated.id ? updated : d))
    );
  }

  ngOnInit(): void {
    this.deviceApi.getAll().subscribe({
      next: (devices) => {
        this.devices.set(devices);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Could not load devices. Is the API running?');
        this.loading.set(false);
        console.error(err);
      },
    });
  }
}

/**
 * Returns true if a device should be counted as "active" in the home summary.
 * - Light/Fan: powered on
 * - Thermostat: actively heating or cooling (idle is NOT active per spec §1.1.3)
 * - DoorLock: never counted as active (it's always doing its job)
 */
function isDeviceActive(d: AnyDevice): boolean {
  switch (d.type) {
    case 'Light':
    case 'Fan':
      return d.powerState === 'On';
    case 'Thermostat':
      return d.state === 'Heating' || d.state === 'Cooling';
    case 'DoorLock':
      return false;
    default:
      return false;
  }


}
