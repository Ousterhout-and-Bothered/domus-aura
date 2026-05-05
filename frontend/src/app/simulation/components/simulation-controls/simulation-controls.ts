import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { ConfirmationService, MessageService } from 'primeng/api';
import { PanelModule } from 'primeng/panel';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';

import { AnyDevice } from '../../../device/models/device-types';
import { DeviceType } from '../../../device/models/device';
import { SimulationApiService } from '../../services/simulation-api.service';

import { Subject, debounceTime } from 'rxjs';

interface SpeedOption {
  label: string;
  value: number;
}

/**
 * Component for controlling the environment simulation.
 *
 * It allows users to adjust the simulation speed, reset all devices to their
 * default states, and set ambient temperatures for different locations.
 * The component also displays a synchronized simulation clock.
 */
@Component({
  selector: 'aura-simulation-controls',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PanelModule,
    SelectModule,
    InputNumberModule,
    ButtonModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-panel
      header="Simulation"
      [toggleable]="true"
      [collapsed]="true"
      styleClass="sim-panel"
    >
      <ng-template pTemplate="icons">
        <span class="sim-clock-inline" [title]="clockTitle()">
          <i class="pi pi-clock"></i>
          {{ formattedClock() }}
        </span>
      </ng-template>

      <div class="sim-grid">
        <section class="sim-section">
          <h3 class="sim-section-title">Speed</h3>
          <p class="sim-section-help">
            How fast simulated time advances relative to real time.
          </p>
          <p-select
            [options]="speedOptions()"
            [ngModel]="currentSpeed()"
            (ngModelChange)="onSpeedChange($event)"
            optionLabel="label"
            optionValue="value"
            placeholder="Loading…"
            [disabled]="loading() || setting()"
            styleClass="sim-control"
          />
        </section>

        <section class="sim-section">
          <h3 class="sim-section-title">Reset</h3>
          <p class="sim-section-help">
            Restore every device to its default state. Settings, names, and
            locations are preserved.
          </p>
          <p-button
            label="Reset all devices"
            icon="pi pi-refresh"
            severity="danger"
            [text]="true"
            [disabled]="loading() || resetting()"
            [loading]="resetting()"
            (onClick)="onResetClick($event)"
          />
        </section>

        @if (thermostatLocations().length > 0) {
          <section class="sim-section sim-section-wide">
            <h3 class="sim-section-title">Ambient temperature</h3>
            <p class="sim-section-help">
              Set the ambient temperature for each room with a thermostat.
              The thermostat will heat or cool to reach its set point.
            </p>
            <div class="sim-temp-grid">
              @for (loc of thermostatLocations(); track loc) {
                <div class="sim-temp-row">
                  <label [attr.for]="ambientInputId(loc)" class="sim-temp-label">
                    {{ loc }}
                  </label>
                  <p-inputnumber
                    [inputId]="ambientInputId(loc)"
                    [ngModel]="ambientTemps()[loc] ?? null"
                    (ngModelChange)="onAmbientChange(loc, $event)"
                    [min]="-40"
                    [max]="150"
                    suffix="°F"
                    [showButtons]="true"
                    buttonLayout="horizontal"
                    incrementButtonIcon="pi pi-plus"
                    decrementButtonIcon="pi pi-minus"
                    styleClass="sim-temp-input"></p-inputnumber>
                </div>
              }
            </div>
          </section>
        }
      </div>
    </p-panel>
  `,
  styleUrl: './simulation-controls.scss',
})
export class SimulationControls implements OnInit {
  private readonly api = inject(SimulationApiService);
  private readonly messages = inject(MessageService);
  private readonly confirms = inject(ConfirmationService);
  private readonly destroyRef = inject(DestroyRef);
  /** Debounces ambient changes so mid-typing values don't round-trip. */
  private readonly ambientChange$ = new Subject<{ location: string; temperature: number }>();
  /** Tracks when the user last edited each location's ambient input.
   Within RECENT_EDIT_WINDOW_MS, the live-sync effect skips that location. */
  private readonly recentEdits = new Map<string, number>();
  private readonly RECENT_EDIT_WINDOW_MS = 2000;

  /* ─────────────── Inputs / outputs ─────────────── */

  /**
   * The current list of devices, used to identify locations for ambient temperature control.
   */
  readonly devices = input.required<AnyDevice[]>();

  /**
   * Emits when the simulation has been reset, prompting the parent to refresh state.
   */
  readonly simulationReset = output<void>();

  /* ─────────────── State ─────────────── */

  readonly loading = signal<boolean>(true);
  readonly setting = signal<boolean>(false);
  readonly resetting = signal<boolean>(false);

  readonly currentSpeed = signal<number | null>(null);
  readonly allowedSpeeds = signal<number[]>([]);

  readonly serverClock = signal<string | null>(null);
  readonly displayClock = signal<Date | null>(null);

  readonly ambientTemps = signal<Record<string, number>>({});

  /* ─────────────── Derived ─────────────── */

  readonly thermostatLocations = computed(() => {
    const set = new Set<string>();
    for (const d of this.devices()) {
      if (d.type === DeviceType.Thermostat) set.add(d.location);
    }
    return Array.from(set).sort((a, b) => a.localeCompare(b));
  });

  readonly speedOptions = computed<SpeedOption[]>(() =>
    this.allowedSpeeds().map((s) => ({
      label: s === 1 ? 'Real-time (1×)' : `${s}× speed`,
      value: s,
    }))
  );

  readonly formattedClock = computed(() => {
    const d = this.displayClock();
    if (!d) return '—';
    return d.toLocaleTimeString(undefined, {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  });

  readonly clockTitle = computed(() => {
    const d = this.displayClock();
    return d ? `Simulation clock: ${d.toLocaleString()}` : 'Simulation clock unavailable';
  });

  /* ─────────────── Lifecycle ─────────────── */

  ngOnInit(): void {
    this.loadInitialState();
    this.startClockTick();
    this.startServerPoll();
  }

  constructor() {
    effect(() => {
      const incoming: Record<string, number> = {};
      const now = Date.now();

      for (const d of this.devices()) {
        if (d.type === DeviceType.Thermostat && !(d.location in incoming)) {
          // Skip locations the user just edited — let their value stand
          const lastEdit = this.recentEdits.get(d.location) ?? 0;
          if (now - lastEdit < this.RECENT_EDIT_WINDOW_MS) continue;
          incoming[d.location] = d.ambientTemperature;
        }
      }

      // Merge, don't replace — preserve in-flight edits for skipped locations
      this.ambientTemps.update((current) => ({ ...current, ...incoming }));
    });

    this.ambientChange$
      .pipe(debounceTime(600), takeUntilDestroyed(this.destroyRef))
      .subscribe(({ location, temperature }) => {
        this.commitAmbientChange(location, temperature);
      });
  }

  /* ─────────────── Loaders ─────────────── */

  private loadInitialState(): void {
    this.loading.set(true);

    this.api.getAllowedSpeeds().subscribe({
      next: (resp) => this.allowedSpeeds.set(resp.speeds),
      error: (err) => {
        console.error('Failed to load allowed speeds', err);
        this.allowedSpeeds.set([1, 2, 5, 10]);
      },
    });

    this.api.getState().subscribe({
      next: (state) => {
        this.currentSpeed.set(state.speedMultiplier);
        this.serverClock.set(state.simulationClock);
        this.displayClock.set(new Date(state.simulationClock));
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load simulation state', err);
        this.loading.set(false);
      },
    });
  }

  /**
   * Animate the displayed clock once per real-time second, scaled by the
   * current speed multiplier. Server poll reconciles drift every 15s.
   */
  private startClockTick(): void {
    interval(1000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const current = this.displayClock();
        const speed = this.currentSpeed() ?? 1;
        if (!current) return;
        this.displayClock.set(new Date(current.getTime() + 1000 * speed));
      });
  }

  /** Reconcile against the server every 15s; corrects local-tick drift. */
  private startServerPoll(): void {
    interval(15_000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.api.getState().subscribe({
          next: (state) => {
            this.serverClock.set(state.simulationClock);
            this.displayClock.set(new Date(state.simulationClock));
            if (this.currentSpeed() !== state.speedMultiplier) {
              this.currentSpeed.set(state.speedMultiplier);
            }
          },
          error: (err) => console.error('Simulation poll failed', err),
        });
      });
  }

  /**
   * Seed ambient inputs from current thermostat readings so the first PUT
   * doesn't replace a meaningful value with whatever the user types.
   */
  private seedAmbientTempsFromDevices(): void {
    const seed: Record<string, number> = {};
    for (const d of this.devices()) {
      if (d.type === DeviceType.Thermostat && !(d.location in seed)) {
        seed[d.location] = d.ambientTemperature;
      }
    }
    this.ambientTemps.set(seed);
  }

  /* ─────────────── Event handlers ─────────────── */

  onSpeedChange(speed: number): void {
    if (speed === this.currentSpeed()) return;

    const previous = this.currentSpeed();
    this.currentSpeed.set(speed);
    this.setting.set(true);

    this.api.setSpeed(speed).subscribe({
      next: () => {
        this.setting.set(false);
        this.messages.add({
          severity: 'success',
          summary: 'Simulation',
          detail: `Speed set to ${speed}×`,
          life: 2000,
        });
      },
      error: (err: HttpErrorResponse) => {
        this.currentSpeed.set(previous);
        this.setting.set(false);
        this.toastError('Could not change speed', err);
      },
    });
  }

  onAmbientChange(location: string, temperature: number): void {
    if (temperature === null || temperature === undefined) return;
    if (Number.isNaN(temperature)) return;

    this.recentEdits.set(location, Date.now());
    this.ambientTemps.update((m) => ({ ...m, [location]: temperature }));
    this.ambientChange$.next({ location, temperature });
  }

    private commitAmbientChange(location: string, temperature: number): void {
        const previous = this.ambientTemps()[location];

        this.api.setAmbientTemperature(location, temperature).subscribe({
          next: () => {},
          error: (err: HttpErrorResponse) => {
            this.ambientTemps.update((m) => {
              const next = { ...m };
              if (previous === undefined) delete next[location];
              else next[location] = previous;
              return next;
            });
            this.toastError(`Could not set ambient for ${location}`, err);
          },
        });
      }

  onResetClick(event: Event): void {
    this.confirms.confirm({
      target: event.currentTarget as HTMLElement,
      header: 'Reset all devices',
      message: 'Restore every device to its default state? Names and locations will be preserved.',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Reset',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => this.executeReset(),
    });
  }

  private executeReset(): void {
    this.resetting.set(true);
    this.api.resetAllDevices().subscribe({
      next: () => {
        this.resetting.set(false);
        this.simulationReset.emit();
        this.messages.add({
          severity: 'success',
          summary: 'Simulation',
          detail: 'All devices reset to defaults',
          life: 2500,
        });
      },
      error: (err: HttpErrorResponse) => {
        this.resetting.set(false);
        this.toastError('Reset failed', err);
      },
    });
  }

  /* ─────────────── Helpers ─────────────── */

  ambientInputId(location: string): string {
    return `ambient-${location.replace(/\s+/g, '-').toLowerCase()}`;
  }

  private toastError(summary: string, err: HttpErrorResponse): void {
    console.error(summary, err);
    const detail = err.error?.detail ?? err.error?.title ?? 'Please try again.';
    this.messages.add({
      severity: 'error',
      summary,
      detail,
      life: 4000,
    });
  }
}
