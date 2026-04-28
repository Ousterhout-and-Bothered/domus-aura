import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { AnyDevice } from '../../models/device-types';
import { DeviceCard } from '../device-card/device-card';

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
          <aura-device-card [device]="device" (deviceUpdated)="deviceUpdated.emit($event)" />
        }
      </div>
    </section>
  `,
  styleUrl: './room-block.scss',
})
export class RoomBlock {
  readonly location = input.required<string>();
  readonly devices = input.required<AnyDevice[]>();

  readonly deviceUpdated = output<AnyDevice>();

  readonly activeCount = computed(() =>
    this.devices().filter((d) => {
      if (d.type === 'Light' || d.type === 'Fan') return d.powerState === 'On';
      if (d.type === 'Thermostat') return d.state === 'Heating' || d.state === 'Cooling';
      return false;
    }).length
  );
}
