import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';

import { MessageService } from 'primeng/api';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';

import { DeviceType, UpdateDeviceRequest } from '../../models/device';
import { AnyDevice } from '../../models/device-types';
import { DeviceApiService } from '../../services/device-api.service';

/**
 * Modal for editing an existing device's name and location.
 *
 * Two fields: Name (text), Location (editable dropdown that allows
 * both selecting an existing room and typing a new one). Type is
 * not editable — changing a device's type is a delete-and-register
 * operation, not an update.
 *
 * Submission rules:
 *   - Both fields required, name trimmed of whitespace
 *   - Location is normalized: case-insensitive match against existing
 *     locations is treated as the existing canonical version
 *   - No-op (no fields changed from the seed device) is allowed; the
 *     backend handles it by returning the device unchanged with no
 *     audit entry
 *   - Backend errors (409 thermostat-conflict on relocate, 400 validation,
 *     404 deleted-since-loaded) display inline and keep the dialog open
 *   - Success closes the dialog, fires a toast, and emits deviceUpdated
 *     so the parent can update its device list optimistically
 *
 * The dialog is *controlled* — its visible state is owned by the parent
 * via [(visible)] two-way binding. The `device` input seeds the initial
 * field values when the dialog opens.
 */
@Component({
  selector: 'aura-edit-device-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DialogModule,
    SelectModule,
    InputTextModule,
    ButtonModule,
    MessageModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [closable]="!submitting()"
      [draggable]="false"
      [resizable]="false"
      header="Edit Device"
      styleClass="edit-dialog"
      [style]="{ width: '32rem' }"
    >
      <form class="edit-form" (ngSubmit)="onSubmit()">
        <div class="form-field">
          <label [attr.for]="nameId">Name</label>
          <input
            pInputText
            [id]="nameId"
            [ngModel]="name()"
            (ngModelChange)="name.set($event)"
            name="name"
            placeholder="e.g. Bedside Lamp"
            maxlength="50"
            [disabled]="submitting()"
            class="form-control"
          />
        </div>

        <div class="form-field">
          <label [attr.for]="locationId">Location</label>
          <p-select
            [inputId]="locationId"
            [options]="locationOptions()"
            [ngModel]="location()"
            (ngModelChange)="location.set($event)"
            name="location"
            placeholder="Choose a room or type a new one"
            optionLabel="label"
            optionValue="value"
            [editable]="true"
            [disabled]="submitting()"
            appendTo="body"
            styleClass="form-control"
          />
          @if (newLocationHint(); as hint) {
            <small class="form-hint">
              <i class="pi pi-plus-circle"></i>
              {{ hint }}
            </small>
          }
        </div>

        @if (errorMessage(); as msg) {
          <p-message severity="error" [text]="msg" styleClass="form-error" />
        }
      </form>

      <ng-template pTemplate="footer">
        <p-button
          label="Cancel"
          severity="secondary"
          [text]="true"
          [disabled]="submitting()"
          (onClick)="onCancel()"
        />
        <p-button
          label="Save Changes"
          icon="pi pi-check"
          [disabled]="!canSubmit()"
          [loading]="submitting()"
          (onClick)="onSubmit()"
        />
      </ng-template>
    </p-dialog>
  `,
  styleUrl: './edit-device-dialog.scss',
})
export class EditDeviceDialog {
  private readonly deviceApi = inject(DeviceApiService);
  private readonly messages = inject(MessageService);

  /* ─────────────── Inputs / outputs ─────────────── */

  readonly visible = input.required<boolean>();
  readonly device = input.required<AnyDevice>();
  readonly existingLocations = input.required<string[]>();

  readonly visibleChange = output<boolean>();
  readonly deviceUpdated = output<AnyDevice>();

  /* ─────────────── Form state ─────────────── */

  readonly name = signal<string>('');
  readonly location = signal<string>('');

  readonly submitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  /* ─────────────── Unique IDs (for label/inputId pairing) ─────────────── */

  private readonly _uid = Math.random().toString(36).slice(2, 9);
  readonly nameId = `edit-name-${this._uid}`;
  readonly locationId = `edit-location-${this._uid}`;

  /* ─────────────── Seed form from input ─────────────── */

  // Re-seeds the form when either the device input changes or the dialog
  // is opened. Ensures the user always sees the device's current values
  // when they hit Edit, including if they had typed something earlier
  // and cancelled out.
  constructor() {
    effect(() => {
      if (this.visible()) {
        const d = this.device();
        this.name.set(d.name);
        this.location.set(d.location);
        this.errorMessage.set(null);
      }
    });
  }

  /* ─────────────── Derived state ─────────────── */

  readonly locationOptions = computed(() =>
    this.existingLocations().map((loc) => ({ label: loc, value: loc }))
  );

  readonly canSubmit = computed(() =>
    !this.submitting()
    && this.name().trim().length > 0
    && this.location().trim().length > 0
  );

  readonly newLocationHint = computed(() => {
    const typed = this.location().trim();
    if (!typed) return null;
    const matchesExisting = this.existingLocations().some(
      (loc) => loc.toLowerCase() === typed.toLowerCase()
    );
    if (matchesExisting) return null;
    return `Will create new room "${typed}"`;
  });

  /* ─────────────── Event handlers ─────────────── */

  onVisibleChange(next: boolean): void {
    if (!next) this.errorMessage.set(null);
    this.visibleChange.emit(next);
  }

  onCancel(): void {
    this.visibleChange.emit(false);
    this.errorMessage.set(null);
  }

  onSubmit(): void {
    if (!this.canSubmit()) return;

    const request = this.buildRequest();
    const id = this.device().id;
    this.submitting.set(true);
    this.errorMessage.set(null);

    this.deviceApi.update(id, request).subscribe({
      next: (updated) => {
        this.deviceUpdated.emit(updated);
        this.messages.add({
          severity: 'success',
          summary: 'Device updated',
          detail: `${updated.name} in ${updated.location}`,
          life: 2500,
        });
        this.submitting.set(false);
        this.visibleChange.emit(false);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  /* ─────────────── Helpers ─────────────── */

  private normalizeLocation(typed: string): string {
    const trimmed = typed.trim();
    const match = this.existingLocations().find(
      (loc) => loc.toLowerCase() === trimmed.toLowerCase()
    );
    return match ?? trimmed;
  }

  private buildRequest(): UpdateDeviceRequest {
    return {
      name: this.name().trim(),
      location: this.normalizeLocation(this.location()),
    };
  }

  /**
   * Translate backend error responses into something the user can act on.
   *
   * 409 → thermostat-already-exists at the target location (relocate
   *       conflict). Surface specifically so the user knows it's the
   *       new location that's the issue, not the old one
   * 404 → device deleted between the time the dialog was opened and
   *       Save was clicked (rare but real, e.g. SSE delete arrived)
   * 400 → validation; if the backend returns a Problem Details body
   *       with a `detail` field, surface that
   * else → generic fallback
   */
  private extractErrorMessage(err: HttpErrorResponse): string {
    const problemDetail = err.error?.detail ?? err.error?.title;

    if (err.status === 409 && this.device().type === DeviceType.Thermostat) {
      return `A thermostat already exists in "${this.location().trim()}". Each room can have at most one thermostat.`;
    }

    if (err.status === 404) {
      return 'This device no longer exists. The window will close.';
    }

    if (err.status === 400) {
      return problemDetail ?? 'Some fields are invalid. Please check and try again.';
    }

    return problemDetail ?? 'Something went wrong. Please try again.';
  }
}
