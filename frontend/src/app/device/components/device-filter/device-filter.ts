import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ToolbarModule } from 'primeng/toolbar';
import { SelectButtonModule } from 'primeng/selectbutton';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { ButtonModule } from 'primeng/button';

import { DeviceType } from '../../models/device';
import {
  DeviceFilters,
  DEFAULT_FILTERS,
  isFilterActive,
} from '../../services/filter';

/**
 * Component providing a filter bar for the device dashboard.
 *
 * It allows users to filter the device list by status (On/Off/All),
 * location, and device type. The filtering logic itself is handled
 * by the parent component or service.
 */
@Component({
  selector: 'aura-device-filters',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ToolbarModule,
    SelectButtonModule,
    SelectModule,
    MultiSelectModule,
    ButtonModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-toolbar styleClass="filter-toolbar">
      <ng-template pTemplate="start">
        <div class="filter-group">
          <span class="filter-label">Status</span>
          <p-selectbutton
            [options]="powerOptions"
            [ngModel]="filters().powerStatus"
            (onChange)="onPowerChange($event.value)"
            [allowEmpty]="false"
            optionLabel="label"
            optionValue="value"
            styleClass="filter-power"></p-selectbutton>
        </div>

        <div class="filter-group">
          <span class="filter-label">Location</span>
          <p-select
            [options]="locationOptions()"
            [ngModel]="filters().location"
            (ngModelChange)="onLocationChange($event)"
            placeholder="All rooms"
            [showClear]="true"
            optionLabel="label"
            optionValue="value"
            class="filter-location"
          />
        </div>

        <div class="filter-group">
          <span class="filter-label">Type</span>
          <p-multiselect
            [options]="typeOptions"
            [ngModel]="selectedTypesArray()"
            (ngModelChange)="onTypesChange($event)"
            placeholder="All types"
            optionLabel="label"
            optionValue="value"
            display="chip"
            styleClass="filter-types"></p-multiselect>
        </div>
      </ng-template>

      <ng-template pTemplate="end">
        @if (anyActive()) {
          <p-button
            label="Clear"
            icon="pi pi-times"
            severity="secondary"
            [text]="true"
            (onClick)="onClear()"
          />
        }
      </ng-template>
    </p-toolbar>
  `,
  styleUrl: './device-filter.scss',
})
export class DeviceFiltersComponent {
  /* ─────────────── Inputs / outputs ─────────────── */

  /** The current state of the device filters. */
  readonly filters = input.required<DeviceFilters>();

  /** The list of available locations to filter by. */
  readonly locations = input.required<string[]>();

  /** Emits when the filter criteria have changed. */
  readonly filtersChange = output<DeviceFilters>();

  /* ─────────────── Static option lists ─────────────── */

  readonly powerOptions = [
    { label: 'All', value: 'all' as const },
    { label: 'On',  value: 'on'  as const },
    { label: 'Off', value: 'off' as const },
  ];

  readonly typeOptions = [
    { label: 'Light',      value: DeviceType.Light },
    { label: 'Fan',        value: DeviceType.Fan },
    { label: 'Thermostat', value: DeviceType.Thermostat },
    { label: 'Door Lock',  value: DeviceType.DoorLock },
  ];

  /* ─────────────── Derived options ─────────────── */

  readonly locationOptions = computed(() =>
    this.locations().map((loc) => ({ label: loc, value: loc }))
  );


  readonly selectedTypesArray = computed(() =>
    Array.from(this.filters().types)
  );

  readonly anyActive = computed(() => isFilterActive(this.filters()));

  /* ─────────────── Event handlers ─────────────── */

  onPowerChange(value: 'all' | 'on' | 'off'): void {
    this.filtersChange.emit({ ...this.filters(), powerStatus: value });
  }

  onLocationChange(value: string | null): void {
    this.filtersChange.emit({ ...this.filters(), location: value });
  }

  onTypesChange(values: DeviceType[]): void {
    this.filtersChange.emit({ ...this.filters(), types: new Set(values) });
  }

  onClear(): void {
    this.filtersChange.emit(DEFAULT_FILTERS);
  }
}
