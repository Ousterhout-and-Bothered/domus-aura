import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { DeviceApiService } from '../../services/device-api.service';
import { DeviceEventService } from '../../services/device-event.service';
import { AnyDevice } from '../../models/device-types';
import { DeviceChangeType, DeviceChangedEvent } from '../../models/device';
import { RoomBlock } from '../room-block/room-block';

/**
 * The /devices route. Fetches every device on init, groups them by
 * location, and renders one RoomBlock per room.
 *
 * Live updates flow in via DeviceEventService. SSE acts as a "something
 * changed" signal — for Updated events we re-fetch the device by id
 * rather than trusting the payload shape, so the canonical wire format
 * stays GET /api/devices/{id}. Created and Deleted events use the
 * payload directly (Created has no other source; Deleted only needs
 * the id).
 *
 * If/when the backend payload contract is locked down (every event
 * carries a complete, $type-tagged device snapshot), flip
 * USE_PAYLOAD_DIRECTLY to true to skip the refetch.
 */
const USE_PAYLOAD_DIRECTLY = false;

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
            <span
              class="live-dot"
              [class.connected]="events.connected()"
              [title]="events.connected() ? 'Live updates connected' : 'Live updates disconnected'"
            ></span>
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
export class DeviceList implements OnInit, OnDestroy {
  private readonly deviceApi = inject(DeviceApiService);
  protected readonly events = inject(DeviceEventService);

  readonly devices = signal<AnyDevice[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

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

  readonly activeCount = computed(() =>
    this.devices().filter((d) => isDeviceActive(d)).length
  );

  constructor() {
    // Reactively merge every SSE event into the devices signal.
    // OnPush + immutable replacement means only the affected card
    // re-renders, not the whole list.
    effect(() => {
      const evt = this.events.lastEvent();
      if (!evt) return;
      this.applyEvent(evt);
    });
  }

  /** Optimistic local update from a successful PUT response. */
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
        // Connect AFTER the first snapshot lands so we never apply an
        // event for a device the list hasn't seen yet.
        this.events.connect();
      },
      error: (err) => {
        this.error.set('Could not load devices. Is the API running?');
        this.loading.set(false);
        console.error(err);
      },
    });
  }

  ngOnDestroy(): void {
    this.events.disconnect();
  }

  private applyEvent(evt: DeviceChangedEvent): void {
    switch (evt.changeType) {
      case DeviceChangeType.Created:
        // Created has no prior state to merge with — the payload IS
        // the new device, so use it directly. The factory's omission
        // of $type isn't fatal here because nothing in the rendering
        // path uses $type today; if/when it does, this is the line
        // that'll start lying.
        this.devices.update((current) => [
          ...current,
          evt.payload as unknown as AnyDevice,
        ]);
        break;

      case DeviceChangeType.Updated:
        if (USE_PAYLOAD_DIRECTLY) {
          this.devices.update((current) =>
            current.map((d) =>
              d.id === evt.deviceId ? (evt.payload as unknown as AnyDevice) : d
            )
          );
        } else {
          // Use the SSE event as a "this id changed" signal and
          // refetch the canonical shape. Insulates us from any
          // payload-shape drift on the backend.
          this.deviceApi.getById(evt.deviceId).subscribe({
            next: (fresh) => {
              this.devices.update((current) =>
                current.map((d) => (d.id === fresh.id ? fresh : d))
              );
            },
            error: (err) => {
              // 404 is expected if the device was deleted between the
              // SSE event firing and the GET landing — drop silently.
              if (err?.status !== 404) console.error('Refetch failed', err);
            },
          });
        }
        break;

      case DeviceChangeType.Deleted:
        this.devices.update((current) =>
          current.filter((d) => d.id !== evt.deviceId)
        );
        break;
    }
  }
}

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
