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
 * Dashboard filter bar. Drives a DeviceFilters object that the parent
 * applies to its device list.
 *
 * State is owned by the parent — this component only renders the
 * current state and emits change events. That means the filter bar
 * is trivially testable in isolation (give it filter state, snapshot
 * the render) and never desynced from what's actually being filtered.
 *
 * The four filters are:
 *   - On/Off (combinable as a single tri-state: All/On/Off)
 *   - Location (single-select)
 *   - Device Type (multi-select)
 * All filters combine with AND.
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
        <div class="filter-start">
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
              class="filter-types"></p-multiselect>
          </div>
        </div>
      </ng-template>

      <ng-template pTemplate="end">
        <div class="filter-end">
          @if (anyActive()) {
            <p-button
              label="Clear"
              icon="pi pi-times"
              severity="secondary"
              [text]="true"
              (onClick)="onClear()"
            />
          }
        </div>
      </ng-template>
    </p-toolbar>
  `,
  styleUrl: './device-filter.scss',
})
export class DeviceFiltersComponent {
  /* ─────────────── Inputs / outputs ─────────────── */

  readonly filters = input.required<DeviceFilters>();

  /** Distinct location strings present in the current device list. */
  readonly locations = input.required<string[]>();

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
