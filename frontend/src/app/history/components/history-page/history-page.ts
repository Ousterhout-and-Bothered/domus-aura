import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageModule } from 'primeng/message';

import { DeviceApiService } from '../../../device/services/device-api.service';
import { AnyDevice } from '../../../device/models/device-types';
import { CommandHistory, HistoryFilters, PagedResult } from '../../../device/models/device';
import { classifyOperation, humanizeOperation } from '../../utils/operation-classifier';

/**
 * The /history route. A filterable, paginated audit feed of every command
 * that has been executed against any device, ordered most recent first.
 *
 * Filter state lives in the URL query string so the view is shareable and
 * deep-linkable. The kebab menu on a device card navigates here with
 * `?deviceId=<id>` to pre-filter the feed; the History tab in the nav
 * navigates here with no filters.
 *
 * Filter changes update the URL, which triggers a refetch. This indirection
 * keeps the URL canonical — back/forward browser navigation works, copy-
 * paste of the URL works, and the component never gets out of sync with
 * what the address bar says.
 */
@Component({
  selector: 'aura-history-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    SelectModule,
    DatePickerModule,
    ButtonModule,
    ProgressSpinnerModule,
    MessageModule,
    DatePipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="history-page">
      <header class="history-head">
        <h1>History</h1>
        @if (page(); as p) {
          <p class="history-summary">
            {{ p.total }} {{ p.total === 1 ? 'entry' : 'entries' }}
            @if (hasActiveFilters()) {
              <span class="muted">· filtered</span>
            }
          </p>
        }
      </header>

      <div class="history-filters">
        <div class="filter-field">
          <label [attr.for]="deviceFilterId">Device</label>
          <p-select
            [inputId]="deviceFilterId"
            [options]="deviceOptions()"
            [ngModel]="deviceFilter()"
            (ngModelChange)="onDeviceFilterChange($event)"
            placeholder="All devices"
            optionLabel="label"
            optionValue="value"
            [showClear]="true"
            appendTo="body"
            styleClass="filter-control"
          />
        </div>

        <div class="filter-field">
          <label [attr.for]="locationFilterId">Location</label>
          <p-select
            [inputId]="locationFilterId"
            [options]="locationOptions()"
            [ngModel]="locationFilter()"
            (ngModelChange)="onLocationFilterChange($event)"
            placeholder="All locations"
            optionLabel="label"
            optionValue="value"
            [showClear]="true"
            appendTo="body"
            styleClass="filter-control"
          />
        </div>

        <div class="filter-field">
          <label [attr.for]="dateFilterId">Date range</label>
          <p-datepicker
            [inputId]="dateFilterId"
            [ngModel]="dateRange()"
            (ngModelChange)="onDateRangeChange($event)"
            selectionMode="range"
            [readonlyInput]="true"
            [showButtonBar]="true"
            placeholder="Any time"
            appendTo="body"
            styleClass="filter-control"
          />
        </div>

        @if (hasActiveFilters()) {
          <p-button
            label="Clear filters"
            icon="pi pi-times"
            severity="secondary"
            [text]="true"
            (onClick)="onClearFilters()"
          />
        }
      </div>

      @if (loading()) {
        <div class="history-loading">
          <p-progressspinner [style]="{ width: '2rem', height: '2rem' }" strokeWidth="4" />
        </div>
      } @else if (errorMessage(); as msg) {
        <p-message severity="error" [text]="msg" styleClass="history-error" />
        <p-button label="Try again" icon="pi pi-refresh" (onClick)="reload()" />
      } @else if (page()?.items?.length === 0) {
        <p class="history-empty">
          @if (hasActiveFilters()) {
            No history matches these filters.
          } @else {
            No history yet. Activity will appear here as devices are used.
          }
        </p>
      } @else if (page(); as p) {
        <p-table
          [value]="p.items"
          [paginator]="true"
          [rows]="p.pageSize"
          [totalRecords]="p.total"
          [first]="(p.page - 1) * p.pageSize"
          [lazy]="true"
          (onPage)="onPageChange($event)"
          [rowsPerPageOptions]="[25, 50, 100]"
          styleClass="history-table"
          [showCurrentPageReport]="true"
          currentPageReportTemplate="Showing {first} to {last} of {totalRecords}"
        >
          <ng-template pTemplate="header">
            <tr>
              <th class="col-time">When</th>
              <th class="col-device">Device</th>
              <th class="col-location">Location</th>
              <th class="col-operation">Operation</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-entry>
            <tr>
              <td class="col-time">
                {{ entry.timestamp | date: 'MMM d, y · HH:mm:ss' }}
              </td>
              <td class="col-device">
                <div class="device-cell">
                  <span class="device-name">{{ entry.deviceName }}</span>
                  <span class="device-type">({{ entry.deviceType }})</span>
                </div>
              </td>
              <td class="col-location">
                {{ entry.deviceLocation }}
              </td>
              <td class="col-operation">
                <div class="op-cell">
                  <span class="op-badge" [attr.data-category]="classify(entry.operation).category">
                    <i class="pi {{ classify(entry.operation).icon }}"></i>
                  </span>
                  <span class="op-text">{{ humanize(entry.operation) }}</span>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      }
    </section>
  `,
  styleUrl: './history-page.scss',
})
export class HistoryPage implements OnInit {
  private readonly deviceApi = inject(DeviceApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  /* ─────────────── Static UI IDs ─────────────── */

  private readonly _uid = Math.random().toString(36).slice(2, 9);
  readonly deviceFilterId = `history-device-${this._uid}`;
  readonly locationFilterId = `history-location-${this._uid}`;
  readonly dateFilterId = `history-date-${this._uid}`;

  /* ─────────────── State signals ─────────────── */

  readonly devices = signal<AnyDevice[]>([]);
  readonly page = signal<PagedResult<CommandHistory> | null>(null);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  /** Live filter values, mirrored from the URL query params. */
  readonly deviceFilter = signal<string | null>(null);
  readonly locationFilter = signal<string | null>(null);
  readonly dateRange = signal<Date[] | null>(null);
  readonly currentPage = signal<number>(1);
  readonly currentPageSize = signal<number>(50);

  /* ─────────────── Derived state ─────────────── */

  readonly deviceOptions = computed(() =>
    this.devices().map((d) => ({
      label: `${d.name} (${d.location})`,
      value: d.id,
    }))
  );

  readonly locationOptions = computed(() => {
    const seen = new Set<string>();
    for (const d of this.devices()) seen.add(d.location);
    return Array.from(seen)
      .sort((a, b) => a.localeCompare(b))
      .map((loc) => ({ label: loc, value: loc }));
  });

  /** Lookup map for rendering device name/type/location in the table. */
  readonly deviceById = computed(() => {
    const map = new Map<string, AnyDevice>();
    for (const d of this.devices()) map.set(d.id, d);
    return map;
  });

  readonly hasActiveFilters = computed(() =>
    this.deviceFilter() !== null
    || this.locationFilter() !== null
    || this.dateRange() !== null
  );

  /* ─────────────── Lifecycle ─────────────── */

  constructor() {
    // React to URL changes — initial load and any subsequent navigation
    // (back/forward, kebab deep-link, programmatic patches) all flow through
    // the same code path. The component reads the URL and refetches.
    this.route.queryParamMap.subscribe((params) => {
      this.applyParamsToState(params.get('deviceId'), params.get('location'),
        params.get('from'), params.get('to'),
        params.get('page'), params.get('pageSize'));
    });

    // Refetch whenever any filter signal changes, but only after the device
    // list is loaded (so the dropdowns have options to render against).
    effect(() => {
      // Read all filter signals so the effect re-runs on any change.
      this.deviceFilter();
      this.locationFilter();
      this.dateRange();
      this.currentPage();
      this.currentPageSize();

      if (untracked(() => this.devices().length === 0)) return;
      this.fetchHistory();
    });
  }

  ngOnInit(): void {
    this.deviceApi.getAll().subscribe({
      next: (devices) => {
        this.devices.set(devices);
        this.fetchHistory();
      },
      error: (err) => {
        console.error('Failed to load devices for history filters', err);
        this.errorMessage.set('Could not load devices. Filters may be incomplete.');
        this.fetchHistory();
      },
    });
  }

  /* ─────────────── Event handlers ─────────────── */

  onDeviceFilterChange(value: string | null): void {
    this.patchUrl({ deviceId: value, page: 1 });
  }

  onLocationFilterChange(value: string | null): void {
    this.patchUrl({ location: value, page: 1 });
  }

  onDateRangeChange(value: Date[] | null): void {
    if (!value || value.length < 2 || !value[0] || !value[1]) {
      this.patchUrl({ from: null, to: null, page: 1 });
      return;
    }
    this.patchUrl({
      from: value[0].toISOString(),
      to: value[1].toISOString(),
      page: 1,
    });
  }

  onPageChange(event: { first: number; rows: number; page?: number }): void {
    const newPage = Math.floor(event.first / event.rows) + 1;
    this.patchUrl({ page: newPage, pageSize: event.rows });
  }

  onClearFilters(): void {
    this.router.navigate(['/history']);
  }

  reload(): void {
    this.fetchHistory();
  }

  classify = classifyOperation;

  humanize = humanizeOperation;

  /* ─────────────── Helpers ─────────────── */

  /**
   * Pushes filter changes into the URL's query string. The router will then
   * fire its queryParamMap subscription, which updates the signals, which
   * triggers the fetch effect. Single-source-of-truth: the URL.
   */
  private patchUrl(patch: Record<string, string | number | null>): void {
    const merged = { ...this.route.snapshot.queryParams };
    for (const [key, value] of Object.entries(patch)) {
      if (value === null) {
        delete merged[key];
      } else {
        merged[key] = String(value);
      }
    }
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: merged,
    });
  }

  private applyParamsToState(
    deviceId: string | null,
    location: string | null,
    from: string | null,
    to: string | null,
    page: string | null,
    pageSize: string | null,
  ): void {
    this.deviceFilter.set(deviceId);
    this.locationFilter.set(location);

    if (from && to) {
      this.dateRange.set([new Date(from), new Date(to)]);
    } else {
      this.dateRange.set(null);
    }

    this.currentPage.set(page ? Math.max(1, Number(page)) : 1);
    this.currentPageSize.set(pageSize ? Math.max(1, Number(pageSize)) : 50);
  }

  private fetchHistory(): void {
    const filters: HistoryFilters = {
      page: this.currentPage(),
      pageSize: this.currentPageSize(),
    };

    const deviceId = this.deviceFilter();
    if (deviceId) filters.deviceId = deviceId;

    const location = this.locationFilter();
    if (location) filters.location = location;

    const range = this.dateRange();
    if (range && range.length === 2 && range[0] && range[1]) {
      filters.from = range[0].toISOString();
      filters.to = range[1].toISOString();
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.deviceApi.getAllHistory(filters).subscribe({
      next: (result) => {
        this.page.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load history', err);
        this.errorMessage.set('Could not load history. Please try again.');
        this.loading.set(false);
      },
    });
  }
}
