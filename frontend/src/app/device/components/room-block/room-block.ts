import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { AnyDevice } from '../../models/device-types';
import { DeviceCard } from '../device-card/device-card';
import { isDeviceOn } from '../../services/filter';

/**
 * Represents a group of devices within a specific room or location.
 *
 * It displays the location name, a summary of active devices, and a grid of
 * `DeviceCard` components for each device in that location.
 */
@Component({
  selector: 'aura-room-block',
  standalone: true,
  imports: [DeviceCard],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="room-block">
      <header class="room-block-head">
        <h2 class="room-block-name">{{ location() }}</h2>
        <span class="room-block-count">{{ devices().length }} {{ devices().length === 1 ? 'device' : 'devices' }}</span>
        @if (activeCount() > 0) {
          <span class="room-block-active">{{ activeCount() }} active</span>
        } @else {
          <span class="room-block-active idle">all quiet</span>
        }
      </header>

      <div class="room-block-grid">
        @for (device of devices(); track device.id) {
          <aura-device-card
            [device]="device"
            (deviceUpdated)="deviceUpdated.emit($event)"
            (deviceRemoved)="deviceRemoved.emit($event)"
          />
        }
      </div>
    </section>
  `,
  styleUrl: './room-block.scss',
})
export class RoomBlock {
  /** The name of the room or location. */
  readonly location = input.required<string>();
  /** The list of devices currently in this location. */
  readonly devices = input.required<AnyDevice[]>();

  /** Emits when a device within this room has been updated. */
  readonly deviceUpdated = output<AnyDevice>();
  /** Emits the unique identifier of a device that has been removed from this room. */
  readonly deviceRemoved = output<string>();

  readonly activeCount = computed(() =>
    this.devices().filter(isDeviceOn).length
  );
}
