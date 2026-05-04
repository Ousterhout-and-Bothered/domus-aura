import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
  viewChildren,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';

import { AnyDevice } from '../../../device/models/device-types';
import { SceneRequest, SceneResponse } from '../../models/scene';
import { SceneApiService } from '../../services/scene-api.service';
import { StagedTargetCard } from '../staged-target-card/staged-target-card';

const MAX_NAME_LENGTH = 100;

/**
 * Modal for creating a new scene. Two internal views — editor (default)
 * and picker — share the same dialog shell. Save assembles the
 * SceneRequest from the staged target cards and POSTs to /api/scenes;
 * on success, emits the new scene to the parent and closes.
 *
 * View switching uses CSS visibility, not @switch / @case. This is
 * deliberate: structural directives destroy and re-create their child
 * components on transition, which would tear down every StagedTargetCard
 * (and its in-progress edits) every time the user opens the picker.
 * Both views render simultaneously; only one is visible at a time.
 *
 * Staged devices are stored as snapshots (full AnyDevice objects)
 * rather than ids. This isolates each staged card from upstream SSE
 * updates to the live device list — a thermostat tick or a brightness
 * change elsewhere can't mutate the stable device reference each
 * card was bound to. The trade-off is that scenes built from very
 * stale snapshots will reflect the device's state at stage-time,
 * not save-time; for a one-screen modal this is the right trade.
 *
 * v1 supports create only. Edit (PUT) will reuse the same dialog by
 * accepting an optional existing scene as input.
 */
@Component({
  selector: 'aura-scene-editor-dialog',
  standalone: true,
  imports: [
    FormsModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    StagedTargetCard,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [closeOnEscape]="true"
      [dismissableMask]="false"
      [draggable]="false"
      [resizable]="false"
      [style]="{ width: '720px', maxWidth: '95vw' }"
      header="New Scene"
      styleClass="scene-editor-dialog"
    >
      <div class="scene-editor-body">
        <div class="scene-editor-view" [class.is-hidden]="view() !== 'editor'">
          <label class="scene-editor-name-label" for="sceneName">
            Scene name
          </label>
          <input
            pInputText
            id="sceneName"
            class="scene-editor-name-input"
            [ngModel]="name()"
            (ngModelChange)="onNameChange($event)"
            [maxlength]="MAX_NAME_LENGTH"
            placeholder="e.g. Goodnight"
            autocomplete="off"
          />

          @if (errorMessage()) {
            <div class="scene-editor-error" role="alert">
              <i class="pi pi-exclamation-triangle"></i>
              <span>{{ errorMessage() }}</span>
            </div>
          }

          @if (stagedDevices().length === 0) {
            <div class="scene-editor-targets-placeholder">
              <p>No devices yet. Click "Add device" to choose one.</p>
            </div>
          } @else {
            <div class="scene-editor-targets-list">
              @for (device of stagedDevices(); track device.id) {
                <aura-staged-target-card
                  [device]="device"
                  (remove)="onRemoveStaged(device.id)"
                />
              }
            </div>
          }

          <button
            type="button"
            class="scene-editor-add"
            (click)="onShowPicker()"
          >
            <i class="pi pi-plus"></i>
            Add device
          </button>
        </div>

        <div class="scene-editor-view" [class.is-hidden]="view() !== 'picker'">
          <div class="picker-header">
            <button
              type="button"
              class="picker-back"
              (click)="onBackToEditor()"
            >
              <i class="pi pi-arrow-left"></i>
              Back
            </button>
            <input
              pInputText
              class="picker-filter"
              [ngModel]="pickerFilter()"
              (ngModelChange)="onPickerFilterChange($event)"
              placeholder="Filter devices…"
              autocomplete="off"
            />
          </div>

          @if (availableDevices().length === 0) {
            <p class="picker-empty">
              @if (pickerFilter().trim().length > 0) {
                No devices match "{{ pickerFilter() }}".
              } @else {
                All devices have been added to this scene.
              }
            </p>
          } @else {
            <ul class="picker-list">
              @for (device of availableDevices(); track device.id) {
                <li>
                  <button
                    type="button"
                    class="picker-item"
                    (click)="onSelectDevice(device.id)"
                  >
                    <span class="picker-item-name">{{ device.name }}</span>
                    <span class="picker-item-meta">
                      {{ device.type }} · {{ device.location }}
                    </span>
                  </button>
                </li>
              }
            </ul>
          }
        </div>
      </div>

      <ng-template pTemplate="footer">
        <p-button
          label="Cancel"
          severity="secondary"
          [text]="true"
          (onClick)="onCancel()"
        />
        <p-button
          label="Save"
          icon="pi pi-check"
          severity="primary"
          [disabled]="!canSave()"
          [loading]="saving()"
          (onClick)="onSave()"
        />
      </ng-template>
    </p-dialog>
  `,
  styleUrl: './scene-editor-dialog.scss',
})
export class SceneEditorDialog {
  // Expose to template.
  protected readonly MAX_NAME_LENGTH = MAX_NAME_LENGTH;

  /* ─────────────── Dependencies ─────────────── */

  private readonly sceneApi = inject(SceneApiService);

  /**
   * Live handles to every rendered StagedTargetCard. Used at Save time
   * to call toActions() on each, and as a reactive source for the
   * action-count check that gates the Save button.
   */
  private readonly stagedCards = viewChildren(StagedTargetCard);

  /* ─────────────── Inputs / outputs ─────────────── */

  readonly visible = input.required<boolean>();
  readonly devices = input.required<readonly AnyDevice[]>();

  readonly visibleChange = output<boolean>();
  readonly sceneCreated = output<SceneResponse>();

  /* ─────────────── Internal state ─────────────── */

  protected readonly view = signal<'editor' | 'picker'>('editor');
  protected readonly name = signal('');

  /**
   * Snapshots of the devices the user has staged for this scene.
   * Captured at add-time and held by reference; not refreshed when
   * the upstream device list changes. See class-level docs.
   */
  protected readonly stagedDevices = signal<readonly AnyDevice[]>([]);

  protected readonly pickerFilter = signal('');
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  /**
   * Save is enabled only when:
   *  - the form has a non-empty trimmed name within the length cap,
   *  - at least one staged target has at least one touched property
   *    (otherwise the backend would 400 on "must contain at least
   *    one action"),
   *  - we're not currently saving.
   */
  protected readonly canSave = computed(() => {
    const trimmed = this.name().trim();
    const hasName = trimmed.length > 0 && trimmed.length <= MAX_NAME_LENGTH;
    if (!hasName) return false;

    if (this.saving()) return false;

    const cards = this.stagedCards();
    if (cards.length === 0) return false;

    return cards.some(c => c.toActions().length > 0);
  });

  /**
   * Devices visible in the picker — those not yet staged, optionally
   * filtered by the picker's search input. Lookup is case-insensitive
   * against name and location.
   */
  protected readonly availableDevices = computed(() => {
    const stagedIds = new Set(this.stagedDevices().map(d => d.id));
    const query = this.pickerFilter().trim().toLowerCase();

    return this.devices().filter(d => {
      if (stagedIds.has(d.id)) return false;
      if (query.length === 0) return true;
      return d.name.toLowerCase().includes(query)
        || d.location.toLowerCase().includes(query);
    });
  });

  /* ─────────────── Event handlers ─────────────── */

  protected onVisibleChange(next: boolean): void {
    if (!next) {
      this.resetState();
    }
    this.visibleChange.emit(next);
  }

  protected onNameChange(next: string): void {
    this.name.set(next);
  }

  protected onShowPicker(): void {
    this.view.set('picker');
  }

  protected onBackToEditor(): void {
    this.view.set('editor');
    this.pickerFilter.set('');
  }

  protected onPickerFilterChange(next: string): void {
    this.pickerFilter.set(next);
  }

  protected onSelectDevice(id: string): void {
    const device = this.devices().find(d => d.id === id);
    if (!device) return;

    this.stagedDevices.update(list => [...list, device]);
    this.view.set('editor');
    this.pickerFilter.set('');
  }

  protected onRemoveStaged(id: string): void {
    this.stagedDevices.update(list => list.filter(d => d.id !== id));
  }

  protected onCancel(): void {
    this.visibleChange.emit(false);
    this.resetState();
  }

  protected onSave(): void {
    const cards = this.stagedCards();
    const actions = cards.flatMap(c => c.toActions());

    // canSave should have prevented this, but defend in depth.
    if (actions.length === 0) {
      this.errorMessage.set('Add at least one action before saving.');
      return;
    }

    const request: SceneRequest = {
      name: this.name().trim(),
      actions,
    };

    this.saving.set(true);
    this.errorMessage.set(null);

    this.sceneApi.create(request).subscribe({
      next: (scene) => {
        this.sceneCreated.emit(scene);
        this.visibleChange.emit(false);
        this.resetState();
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  /* ─────────────── Helpers ─────────────── */

  /**
   * Pulls a user-facing error message from an HttpErrorResponse.
   * Prefers the API's Problem Details `detail` field; falls back to
   * `title`, then to a generic message.
   */
  private extractErrorMessage(err: HttpErrorResponse): string {
    const body = err.error;
    if (body && typeof body === 'object') {
      if (typeof body.detail === 'string' && body.detail.length > 0) {
        return body.detail;
      }
      if (typeof body.title === 'string' && body.title.length > 0) {
        return body.title;
      }
    }
    return 'Could not save the scene. Please try again.';
  }

  /**
   * Resets internal state when the dialog closes so a subsequent open
   * starts fresh. Called from Cancel and from the dismiss handler.
   */
  private resetState(): void {
    this.view.set('editor');
    this.name.set('');
    this.stagedDevices.set([]);
    this.pickerFilter.set('');
    this.saving.set(false);
    this.errorMessage.set(null);
  }
}
