import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';

import {
  AnyDevice,
  DoorLock as DoorLockDevice,
  Fan,
  Light,
  Thermostat,
  isDoorLock,
  isFan,
  isLight,
  isThermostat,
} from '../../../device/models/device-types';
import { DeviceType } from '../../../device/models/device';
import { LightBulb } from '../../../device/components/light-bulb/light-bulb';
import { FanSpinning } from '../../../device/components/fan-spinning/fan-spinning';
import { ThermostatGauge } from '../../../device/components/thermostat-gauge/thermostat-gauge';
import { DoorLock } from '../../../device/components/door-lock/door-lock';
import { RecipeStep } from '../../services/scene-recipe';

const STEP_TIMEOUT_MS = 1500;
const STEP_HOLD_MS = 700;
const AUTO_CLOSE_MS = 1500;
const REVEAL_DELAY_MS = 1500;

/**
 * Paced playback dialog for scene execution. Walks through the scene's
 * recipe one step at a time, featuring the affected device with the
 * same leaf visual the dashboard uses.
 *
 * Each step plays in two beats:
 *   1. Before-beat — render the device's pre-execution state from a
 *      snapshot captured at playback start. Held for REVEAL_DELAY_MS so
 *      the user registers what the device looked like before the action.
 *   2. After-beat — switch the binding to the live device from the
 *      parent's allDevices() input. The leaf visual's inputs change,
 *      its CSS transitions engage, and the user sees the change happen.
 *      Held for the remainder of the step duration before advancing.
 *
 * For chained same-device steps (Movie Night: SetPower On then SetSpeed
 * High on the same fan), the second step skips the before-beat — there
 * is no fresh "before" to show, since the previous step just produced
 * the new starting state. The leaf stays mounted and its inputs change
 * directly.
 *
 * The dialog itself never subscribes to SSE. allDevices() is owned by
 * SceneList and updated there. We only read it.
 */
@Component({
  selector: 'aura-scene-execution-dialog',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    LightBulb,
    FanSpinning,
    ThermostatGauge,
    DoorLock,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [closable]="true"
      [draggable]="false"
      [resizable]="false"
      [showHeader]="false"
      [dismissableMask]="true"
      styleClass="scene-execution-dialog"
      [style]="{ width: '420px' }"
    >
      @if (currentStep(); as step) {
        <div class="dlg-shell">
          <header class="dlg-head">
            <p class="dlg-progress">
              Step {{ step.ordinal }} of {{ steps().length }} ·
              {{ sceneName() }}
            </p>
            <h3 class="dlg-title">{{ activeLabel() }}</h3>
          </header>

          <div
            class="dlg-stage"
            [class.is-fading]="fading()"
          >
            @if (boundDevice(); as device) {
              <div class="dlg-card">
                @switch (device.type) {
                  @case (DeviceType.Light) {
                    @if (isLight(device)) {
                      @let l = asLight(device);
                      <aura-light-bulb
                        [name]="l.name"
                        [location]="l.location"
                        [powerState]="l.powerState"
                        [brightness]="l.brightness"
                        [colorHex]="l.colorHex"
                      />
                    }
                  }
                  @case (DeviceType.Fan) {
                    @if (isFan(device)) {
                      @let f = asFan(device);
                      <aura-fan-spinning
                        [name]="f.name"
                        [location]="f.location"
                        [powerState]="f.powerState"
                        [speed]="f.speed"
                      />
                    }
                  }
                  @case (DeviceType.Thermostat) {
                    @if (isThermostat(device)) {
                      @let t = asThermostat(device);
                      <aura-thermostat-gauge
                        [name]="t.name"
                        [location]="t.location"
                        [desiredTemperature]="t.desiredTemperature"
                        [ambientTemperature]="t.ambientTemperature"
                        [mode]="t.mode"
                        [state]="t.state"
                      />
                    }
                  }
                  @case (DeviceType.DoorLock) {
                    @if (isDoorLock(device)) {
                      @let dl = asDoorLock(device);
                      <aura-door-lock
                        [name]="dl.name"
                        [location]="dl.location"
                        [lockState]="dl.lockState"
                      />
                    }
                  }
                }
              </div>
            } @else {
              <p class="dlg-missing">Device no longer registered.</p>
            }
          </div>

          <footer class="dlg-foot">
            @for (s of steps(); track s.ordinal) {
              <span
                class="dot"
                [class.done]="s.ordinal < step.ordinal"
                [class.active]="s.ordinal === step.ordinal"
              ></span>
            }
          </footer>
        </div>
      }
    </p-dialog>
  `,
  styleUrl: './scene-execution-dialog.scss',
})
export class SceneExecutionDialog {
  /** Whether the dialog is open. Two-way bound. */
  readonly visible = input.required<boolean>();
  /** Scene name for the header subtitle. */
  readonly sceneName = input.required<string>();
  /** The recipe to play through. Set when execute starts; do not mutate during playback. */
  readonly steps = input.required<readonly RecipeStep[]>();
  /** Live device list owned by SceneList — updated by SSE in parent. */
  readonly allDevices = input.required<readonly AnyDevice[]>();

  readonly visibleChange = output<boolean>();

  /** Current step index, 0-based. Driven by the pacing tick. */
  readonly currentStepIndex = signal(0);
  /** True during a cross-fade between steps that target different devices. */
  readonly fading = signal(false);
  /**
   * Whether the current step has revealed its post-execution state.
   * False at step entry — the leaf visual binds to the snapshot ("before").
   * True after REVEAL_DELAY_MS — leaf rebinds to live data ("after").
   * The transition between the two is what the user sees as "the action."
   */
  readonly revealed = signal(false);

  /**
   * Pre-execution device snapshot, keyed by deviceId. Captured once at
   * playback-start so the live SSE-driven updates can't mutate it.
   * Used for the "before" half of each step's two-beat presentation.
   */
  private snapshot = new Map<string, AnyDevice>();

  readonly currentStep = computed<RecipeStep | null>(() => {
    const list = this.steps();
    const idx = this.currentStepIndex();
    return idx >= 0 && idx < list.length ? list[idx] : null;
  });

  /**
   * The device the leaf visual binds to. Switches between the snapshot
   * (before-beat) and the live registry (after-beat) based on revealed().
   */
  readonly boundDevice = computed<AnyDevice | null>(() => {
    const step = this.currentStep();
    if (!step || !step.deviceId) return null;

    const fromSnapshot = this.snapshot.get(step.deviceId);
    const fromLive = this.allDevices().find(d => d.id === step.deviceId);
    const result = this.revealed() ? fromLive ?? null : fromSnapshot ?? null;

    console.log('[dlg] step', step.ordinal,
      'revealed=', this.revealed(),
      'snapshot.powerState=', (fromSnapshot as any)?.powerState,
      'live.powerState=', (fromLive as any)?.powerState,
      'using=', this.revealed() ? 'live' : 'snapshot');

    return result;
  });

  /** Step label — falls back to the step's stored label. */
  readonly activeLabel = computed(() => this.currentStep()?.label ?? '');

  /* ─────────────── Type helpers ─────────────── */

  readonly DeviceType = DeviceType;
  readonly isLight = isLight;
  readonly isFan = isFan;
  readonly isThermostat = isThermostat;
  readonly isDoorLock = isDoorLock;

  asLight(d: AnyDevice): Light { return d as Light; }
  asFan(d: AnyDevice): Fan { return d as Fan; }
  asThermostat(d: AnyDevice): Thermostat { return d as Thermostat; }
  asDoorLock(d: AnyDevice): DoorLockDevice { return d as DoorLockDevice; }

  /* ─────────────── Pacing ─────────────── */

  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      const visible = this.visible();
      const stepCount = this.steps().length;

      untracked(() => {
        if (visible && stepCount > 0) {
          this.startPlayback();
        } else {
          this.cancelPending();
          this.currentStepIndex.set(0);
          this.revealed.set(false);
          this.snapshot = new Map();
        }
      });
    });
  }

  readonly beforeDevices = input.required<readonly AnyDevice[]>();

  private startPlayback(): void {
    this.cancelPending();
    this.currentStepIndex.set(0);
    this.revealed.set(false);

    const before = this.beforeDevices();
    console.log('[dlg] startPlayback. beforeDevices count:', before.length,
      'first few:', before.slice(0, 4).map(d => ({id: d.id, ps: (d as any).powerState})));

    this.snapshot = new Map(
      before.map(d => [d.id, d] as const)
    );

    this.scheduleAdvance();
  }

  /**
   * Two-beat per-step pacing.
   *
   * If the current step targets the same device as the previous step
   * (chained same-device, e.g. SetPower On then SetSpeed High on one
   * fan), there is no "before" to show — the previous step's reveal is
   * already the starting state. We skip the before-beat by setting
   * revealed=true immediately. The leaf visual stays mounted across
   * both steps; its inputs change, its CSS transitions engage.
   *
   * Otherwise we play the full two-beat sequence:
   *   1. revealed=false: leaf binds to snapshot, held REVEAL_DELAY_MS.
   *   2. revealed=true: leaf binds to live device, held the remainder
   *      of STEP_TIMEOUT_MS + STEP_HOLD_MS - REVEAL_DELAY_MS.
   * Then advance to the next step (or auto-close if last).
   */
  private scheduleAdvance(): void {
    const idx = this.currentStepIndex();
    const list = this.steps();
    const isLast = idx >= list.length - 1;

    const isChainedSameDevice = idx > 0 &&
      list[idx - 1].deviceId === list[idx].deviceId &&
      list[idx].deviceId !== '';

    if (isChainedSameDevice) {
      // No before-beat. Hold the live state for the full step duration,
      // then advance. The leaf sees its inputs change as soon as the
      // previous step's `revealed` propagated, which already happened.
      this.revealed.set(true);
      this.timer = setTimeout(() => {
        this.afterHoldComplete(idx, list, isLast);
      }, STEP_TIMEOUT_MS + STEP_HOLD_MS);
      return;
    }

    // Two-beat: before, then reveal, then advance.
    this.revealed.set(false);
    this.timer = setTimeout(() => {
      this.revealed.set(true);
      this.timer = setTimeout(() => {
        this.afterHoldComplete(idx, list, isLast);
      }, STEP_TIMEOUT_MS + STEP_HOLD_MS - REVEAL_DELAY_MS);
    }, REVEAL_DELAY_MS);
  }

  /**
   * Common tail for both pacing branches — decides whether to advance,
   * cross-fade, or auto-close after a step's hold completes.
   */
  private afterHoldComplete(
    idx: number,
    list: readonly RecipeStep[],
    isLast: boolean,
  ): void {
    if (isLast) {
      this.timer = setTimeout(() => {
        this.onVisibleChange(false);
      }, AUTO_CLOSE_MS);
      return;
    }

    const next = list[idx + 1];
    const current = list[idx];
    const sameDevice = next.deviceId && next.deviceId === current.deviceId;

    if (sameDevice) {
      this.currentStepIndex.set(idx + 1);
      this.scheduleAdvance();
    } else {
      this.fading.set(true);
      this.timer = setTimeout(() => {
        this.currentStepIndex.set(idx + 1);
        this.fading.set(false);
        this.scheduleAdvance();
      }, 250);
    }
  }

  private cancelPending(): void {
    if (this.timer !== null) {
      clearTimeout(this.timer);
      this.timer = null;
    }
    this.fading.set(false);
  }

  onVisibleChange(open: boolean): void {
    if (!open) this.cancelPending();
    this.visibleChange.emit(open);
  }
}
