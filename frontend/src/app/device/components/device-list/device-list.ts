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
import { RegisterDeviceDialog } from '../register-device-dialog/register-device-dialog';
import { ButtonModule } from 'primeng/button';
import { SimulationControls } from '../../../simulation/components/simulation-controls/simulation-controls';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  DeviceFilters,
  DEFAULT_FILTERS,
  applyFilters,
  availableLocations,
  isDeviceOn,
} from '../../services/filter';

/**
 * Main dashboard component representing the `/devices` route.
 *
 * Fetches all devices on initialization, applies active filters, groups results
 * by location, and renders a `RoomBlock` for each room. It also manages the
 * real-time device event connection.
 */
@Component({
  selector: 'aura-device-list',
  standalone: true,
  imports: [RoomBlock, DeviceFiltersComponent, RegisterDeviceDialog, SimulationControls, ButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="device-list">
      <header class="device-list-header">
        <div class="device-list-titles">
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
        </div>
        <p-button
          label="Add Device"
          icon="pi pi-plus"
          (onClick)="onOpenRegisterDialog()"
        />
      </header>

      @if (devices().length > 0) {
        <aura-simulation-controls
          [devices]="devices()"
          (simulationReset)="onSimulationReset()"
        />

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
      } @else if (rooms().length === 0) {
        <p class="device-list-status muted">
          No devices yet. Click "Add Device" to register your first one.
        </p>
      } @else {
        @for (room of rooms(); track room.location) {
          <aura-room-block
            [location]="room.location"
            [devices]="room.devices"
            (deviceUpdated)="onDeviceUpdated($event)"
            (deviceRemoved)="onDeviceRemoved($event)"
          />
        }
      }

      <aura-register-device-dialog
        [visible]="registerDialogVisible()"
        [existingLocations]="allLocations()"
        (visibleChange)="registerDialogVisible.set($event)"
        (deviceCreated)="onDeviceCreated($event)"
      />
    </section>
  `,
  styleUrl: './device-list.scss',
})
export class DeviceList implements OnInit, OnDestroy {
  private readonly deviceApi = inject(DeviceApiService);
  protected readonly events = inject(DeviceEventService);

  /* ─────────────── State signals ─────────────── */

  /**
   * List of all devices fetched from the API.
   */
  readonly devices = signal<AnyDevice[]>([]);

  /**
   * Indicates if the device list is currently being loaded.
   */
  readonly loading = signal(true);

  /**
   * Holds any error message encountered during device loading.
   */
  readonly error = signal<string | null>(null);

  /**
   * The current set of active device filters.
   */
  readonly filters = signal<DeviceFilters>(DEFAULT_FILTERS);

  /**
   * Controls the visibility of the "Add Device" dialog.
   */
  readonly registerDialogVisible = signal<boolean>(false);

  /* ─────────────── Derived signals ─────────────── */

  /** All distinct locations from the unfiltered device list — feeds the Location dropdown. */
  readonly allLocations = computed(() => availableLocations(this.devices()));

  /** Devices passing the current filters. */
  readonly filteredDevices = computed(() =>
    applyFilters(this.devices(), this.filters())
  );

  /**
   * Filtered devices grouped by location and sorted alphabetically.
   * The room list excludes locations that have no matching devices
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
    this.events.events$
      .pipe(takeUntilDestroyed())
      .subscribe((evt) => this.applyEvent(evt));
  }


  /**
   * Handles device state updates from child components.
   *
   * @param updated - The updated device object.
   */
  onDeviceUpdated(updated: AnyDevice): void {
    this.devices.update((current) =>
      current.map((d) => (d.id === updated.id ? updated : d))
    );
  }


  /**
   * Updates the active filters and triggers a re-filtering of devices.
   *
   * @param next - The new set of filters to apply.
   */
  onFiltersChange(next: DeviceFilters): void {
    this.filters.set(next);
  }

  /** Open the Add Device dialog. */
  onOpenRegisterDialog(): void {
    this.registerDialogVisible.set(true);
  }


  /**
   * Handles the registration of a new device.
   *
   * @param created - The newly created device object.
   */
  onDeviceCreated(created: AnyDevice): void {
    this.devices.update((current) => {
      if (current.some((d) => d.id === created.id)) return current;
      return [...current, created];
    });
  }

  /**
   * Removes a device from the local list.
   *
   * @param deviceId - The unique identifier of the device to remove.
   */
  onDeviceRemoved(deviceId: string): void {
    this.devices.update((current) => current.filter((d) => d.id !== deviceId));
  }

  /**
   * Reset doesn't change the device set — names, locations, and IDs are
   * preserved — so refetch device state rather than reloading the page.
   */
  onSimulationReset(): void {
    this.deviceApi.getAll().subscribe({
      next: (fresh) => this.devices.set(fresh),
      error: (err) => console.error('Refetch after reset failed', err),
    });
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
      case DeviceChangeType.Created: {
        // Dedup on id — the optimistic insert from onDeviceCreated may
        // have already added this device.
        const incoming = evt.payload as unknown as AnyDevice;
        this.devices.update((current) =>
          current.some((d) => d.id === incoming.id)
            ? current
            : [...current, incoming]
        );
        break;
      }

      case DeviceChangeType.Updated:
        this.devices.update((current) =>
          current.map((d) =>
            d.id === evt.deviceId ? (evt.payload as unknown as AnyDevice) : d
          )
        );
        break;

      case DeviceChangeType.Deleted:
        this.devices.update((current) =>
          current.filter((d) => d.id !== evt.deviceId)
        );
        break;
    }
  }
}
