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
import { DeviceFiltersComponent } from '../device-filter/device-filter';
import {
  DeviceFilters,
  DEFAULT_FILTERS,
  applyFilters,
  availableLocations,
  isDeviceOn,
} from '../../services/filter';

/**
 * The /devices route. Fetches every device on init, applies the active
 * filters, groups results by location, and renders one RoomBlock per room.
 *
 * Owns the filter state.
 *
 * Live updates flow in via DeviceEventService. SSE acts as a "something
 * changed" signal — for Updated events we re-fetch the device by id
 * rather than trusting the payload shape, so the canonical wire format
 * stays GET /api/devices/{id}. Created and Deleted events use the
 * payload directly (Created has no other source; Deleted only needs
 * the id).
 */
const USE_PAYLOAD_DIRECTLY = false;

@Component({
  selector: 'aura-device-list',
  standalone: true,
  imports: [RoomBlock, DeviceFiltersComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="device-list">
      <header class="device-list-header">
        <h1>Your Home</h1>
        @if (devices().length > 0) {
          <p class="device-list-summary">
            {{ devices().length }} devices · {{ allLocations().length }} rooms · {{ activeCount() }} active
            <span
              class="live-dot"
              [class.connected]="events.connected()"
              [title]="events.connected() ? 'Live updates connected' : 'Live updates disconnected'"
            ></span>
          </p>
        }
      </header>

      @if (devices().length > 0) {
        <aura-device-filters
          [filters]="filters()"
          [locations]="allLocations()"
          (filtersChange)="onFiltersChange($event)"
        />
      }

      @if (loading()) {
        <p class="device-list-status">Loading devices…</p>
      } @else if (error()) {
        <p class="device-list-status error">{{ error() }}</p>
      } @else if (rooms().length === 0 && devices().length > 0) {
        <p class="device-list-status muted">
          No devices match the current filters.
        </p>
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

  /* ─────────────── State signals ─────────────── */

  readonly devices = signal<AnyDevice[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  /** Active filter state. Mutated only via onFiltersChange. */
  readonly filters = signal<DeviceFilters>(DEFAULT_FILTERS);

  /* ─────────────── Derived signals ─────────────── */

  /** All distinct locations from the unfiltered device list — feeds the Location dropdown. */
  readonly allLocations = computed(() => availableLocations(this.devices()));

  /** Devices passing the current filters. */
  readonly filteredDevices = computed(() =>
    applyFilters(this.devices(), this.filters())
  );

  /**
   * Filtered devices grouped by location and sorted alphabetically.
   * The room list excludes locations that have no matching devices —
   * if you filter to "On + Lights", a room with only an off lamp
   * disappears entirely (rather than rendering an empty room block).
   */
  readonly rooms = computed(() => {
    const byLocation = new Map<string, AnyDevice[]>();
    for (const device of this.filteredDevices()) {
      const list = byLocation.get(device.location) ?? [];
      list.push(device);
      byLocation.set(device.location, list);
    }
    return Array.from(byLocation.entries())
      .map(([location, devices]) => ({ location, devices }))
      .sort((a, b) => a.location.localeCompare(b.location));
  });

  readonly activeCount = computed(() =>
    this.devices().filter(isDeviceOn).length
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

  onDeviceUpdated(updated: AnyDevice): void {
    this.devices.update((current) =>
      current.map((d) => (d.id === updated.id ? updated : d))
    );
  }

  onFiltersChange(next: DeviceFilters): void {
    this.filters.set(next);
  }

  ngOnInit(): void {
    this.deviceApi.getAll().subscribe({
      next: (devices) => {
        this.devices.set(devices);
        this.loading.set(false);
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
          this.deviceApi.getById(evt.deviceId).subscribe({
            next: (fresh) => {
              this.devices.update((current) =>
                current.map((d) => (d.id === fresh.id ? fresh : d))
              );
            },
            error: (err) => {
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
