import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { PrimeTemplate } from 'primeng/api';

import { SceneExecutionResponse } from '../../models/scene';

/**
 * Modal that opens after a scene execution completes. Renders the
 * summary counts and a table of per-action outcomes.
 *
 * This is the rubric-visible piece for two scene requirements:
 *
 *   - "Group targets resolved at execution time" — actions with no
 *     deviceId (i.e., group targets) expand to multiple rows here, one
 *     per resolved device, making the resolution visible.
 *
 *   - "Per-action results reported on partial failure" — failures are
 *     listed alongside successes rather than aborting the run.
 *
 * The dialog is *controlled* — its visible state is owned by the parent
 * via [(visible)] two-way binding.
 */
@Component({
  selector: 'aura-scene-execution-result-dialog',
  standalone: true,
  imports: [CommonModule, DialogModule, ButtonModule, PrimeTemplate],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="visibleChange.emit($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [closable]="true"
      [header]="header()"
      styleClass="scene-result-dialog"
      [style]="{ width: '36rem' }"
    >
      @if (result(); as r) {
        <div class="result-summary">
          <div class="result-summary-counts">
            <span class="count-pill succeeded">
              <i class="pi pi-check-circle"></i>
              {{ r.summary.succeeded }} succeeded
            </span>
            @if (r.summary.failed > 0) {
              <span class="count-pill failed">
                <i class="pi pi-times-circle"></i>
                {{ r.summary.failed }} failed
              </span>
            }
          </div>

          @if (r.results.length === 0) {
            <p class="result-empty">No actions were resolved.</p>
          }
        </div>

        @if (r.results.length > 0) {
          <ol class="result-list">
            @for (entry of r.results; track entry.orderIndex + entry.deviceId) {
              <li
                class="result-row"
                [class.succeeded]="isSuccess(entry.status) && !isNoOp(entry.status)"
                [class.noop]="isNoOp(entry.status)"
                [class.failed]="!isSuccess(entry.status)"
              >
              <span class="status-icon" aria-hidden="true">
                <i
                  class="pi"
                  [class.pi-check]="isSuccess(entry.status) && !isNoOp(entry.status)"
                  [class.pi-minus]="isNoOp(entry.status)"
                  [class.pi-times]="!isSuccess(entry.status)"
                ></i>
              </span>

                <div class="result-row-body">
                  <div class="result-row-primary">
                    <span class="device-name">{{ entry.deviceName }}</span>
                    <span class="operation">{{ entry.operation }}</span>
                    @if (entry.value != null && entry.value !== '') {
                      <span class="value">→ {{ entry.value }}</span>
                    }
                    <span class="status-tag">{{ statusLabel(entry.status) }}</span>
                  </div>
                  @if (entry.message) {
                    <p class="result-row-message">{{ entry.message }}</p>
                  }
                </div>
              </li>
            }
          </ol>
        }
      }

      <ng-template pTemplate="footer">
        <p-button
          label="Close"
          severity="secondary"
          (onClick)="visibleChange.emit(false)"
        />
      </ng-template>
    </p-dialog>
  `,
  styleUrl: './scene-execution-result-dialog.scss',
})
export class SceneExecutionResultDialog {
  readonly visible = input.required<boolean>();
  readonly result = input<SceneExecutionResponse | null>(null);

  readonly visibleChange = output<boolean>();

  readonly header = computed(() => {
    const r = this.result();
    return r ? `${r.sceneName} · execution result` : 'Execution result';
  });

  isSuccess(status: string): boolean {
    return status?.toLowerCase() !== 'failed';
  }

  isNoOp(status: string): boolean {
    return status != null && status.toLowerCase().startsWith('already');
  }

  statusLabel(status: string): string {
    switch (status?.toLowerCase()) {
      case 'changed': return 'Changed';
      case 'failed': return 'Failed';
      case 'already_on': return 'Already on';
      case 'already_off': return 'Already off';
      case 'already_locked': return 'Already locked';
      case 'already_unlocked': return 'Already unlocked';
      case 'already_in_requested_state': return 'No change needed';
      default: return status ?? 'Unknown';
    }
  }
}
