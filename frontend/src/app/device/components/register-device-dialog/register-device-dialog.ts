import {
  ChangeDetectionStrategy,
  Component,
  computed,
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

import { DeviceType, RegisterDeviceRequest } from '../../models/device';
import { AnyDevice } from '../../models/device-types';
import { DeviceApiService } from '../../services/device-api.service';

/**
 * Modal for registering a new device.
 *
 * Three fields: Type (dropdown), Name (text), Location (editable dropdown
 * that allows both selecting an existing room and typing a new one).
 *
 * Submission rules:
 *   - All three fields required, name trimmed of whitespace
 *   - Location is normalized: case-insensitive match against existing
 *     locations is treated as the existing canonical version
 *   - Backend errors (409 thermostat conflict, 400 validation) display
 *     inline and keep the dialog open so the user can fix and retry
 *   - Success closes the dialog, fires a toast, and emits deviceCreated
 *     so the parent can update its device list optimistically (the SSE
 *     event will also arrive, but the optimistic update keeps the UI
 *     responsive)
 *
 * The dialog is *controlled* — its visible state is owned by the parent
 * via [(visible)] two-way binding. We open it by setting visible=true
 * from the parent and close it by setting visible=false (either from a
 * successful submit, the cancel button, or the X in the dialog header).
 */
@Component({
  selector: 'aura-register-device-dialog',
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
      header="Add Device"
      styleClass="register-dialog"
      [style]="{ width: '32rem' }"
    >
      <form class="register-form" (ngSubmit)="onSubmit()">
        <div class="form-field">
          <label [attr.for]="typeId">Type</label>
          <p-select
            [inputId]="typeId"
            [options]="typeOptions"
            [ngModel]="type()"
            (ngModelChange)="type.set($event)"
            name="type"
            placeholder="Choose a device type"
            optionLabel="label"
            optionValue="value"
            [disabled]="submitting()"
            appendTo="body"
            styleClass="form-control"
          />
        </div>

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
          label="Add Device"
          icon="pi pi-plus"
          [disabled]="!canSubmit()"
          [loading]="submitting()"
          (onClick)="onSubmit()"
        />
      </ng-template>
    </p-dialog>
  `,
  styleUrl: './register-device-dialog.scss',
})
export class RegisterDeviceDialog {
  private readonly deviceApi = inject(DeviceApiService);
  private readonly messages = inject(MessageService);

  /* ─────────────── Inputs / outputs ─────────────── */

  readonly visible = input.required<boolean>();
  readonly existingLocations = input.required<string[]>();

  readonly visibleChange = output<boolean>();
  readonly deviceCreated = output<AnyDevice>();

  /* ─────────────── Form state ─────────────── */

  readonly type = signal<DeviceType | null>(null);
  readonly name = signal<string>('');
  readonly location = signal<string>('');

  readonly submitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  /* ─────────────── Static option lists ─────────────── */

  readonly typeOptions = [
    { label: 'Light',      value: DeviceType.Light },
    { label: 'Fan',        value: DeviceType.Fan },
    { label: 'Thermostat', value: DeviceType.Thermostat },
    { label: 'Door Lock',  value: DeviceType.DoorLock },
  ];

  /* ─────────────── Unique IDs (for label/inputId pairing) ─────────────── */

  private readonly _uid = Math.random().toString(36).slice(2, 9);
  readonly typeId = `register-type-${this._uid}`;
  readonly nameId = `register-name-${this._uid}`;
  readonly locationId = `register-location-${this._uid}`;

  /* ─────────────── Derived state ─────────────── */

  readonly locationOptions = computed(() =>
    this.existingLocations().map((loc) => ({ label: loc, value: loc }))
  );

  /**
   * Form is submittable when all three fields have content. We compare
   * trimmed values so a name of just spaces doesn't count.
   */
  readonly canSubmit = computed(() =>
    !this.submitting()
    && this.type() !== null
    && this.name().trim().length > 0
    && this.location().trim().length > 0
  );

  /**
   * If the user has typed a location that doesn't match any existing
   * room (case-insensitive), surface a hint that they're about to
   * create a new room. Catches typos before submission.
   */
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
    if (!next) this.resetForm();
    this.visibleChange.emit(next);
  }

  onCancel(): void {
    this.visibleChange.emit(false);
    this.resetForm();
  }

  onSubmit(): void {
    if (!this.canSubmit()) return;

    const request = this.buildRequest();
    this.submitting.set(true);
    this.errorMessage.set(null);

    this.deviceApi.register(request).subscribe({
      next: (created) => {
        this.deviceCreated.emit(created);
        this.messages.add({
          severity: 'success',
          summary: 'Device added',
          detail: `${created.name} in ${created.location}`,
          life: 2500,
        });
        this.submitting.set(false);
        this.visibleChange.emit(false);
        this.resetForm();
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  /* ─────────────── Helpers ─────────────── */

  /**
   * Normalize the typed location against existing rooms. Trim whitespace
   * and case-insensitively match against existing — if found, use the
   * canonical casing. Otherwise treat as a new room with typed casing.
   *
   * Example: existing = ["Living Room", "Kitchen"]
   *   typed " kitchen "  → "Kitchen" (matched, normalized)
   *   typed "Garage"     → "Garage"  (new)
   *   typed "GARAGE"     → "GARAGE"  (new, casing preserved)
   */
  private normalizeLocation(typed: string): string {
    const trimmed = typed.trim();
    const match = this.existingLocations().find(
      (loc) => loc.toLowerCase() === trimmed.toLowerCase()
    );
    return match ?? trimmed;
  }

  private buildRequest(): RegisterDeviceRequest {
    return {
      type: this.type()!,
      name: this.name().trim(),
      location: this.normalizeLocation(this.location()),
    };
  }

  /**
   * Translate backend error responses into something the user can act on.
   *
   * 409 → thermostat-already-exists is the only documented conflict;
   *       other 409s would still be conflict-class issues, so we fall
   *       through to a generic "already exists" message
   * 400 → validation; if the backend returns a Problem Details body
   *       with a `detail` field, surface that
   * else → generic fallback
   */
  private extractErrorMessage(err: HttpErrorResponse): string {
    const problemDetail = err.error?.detail ?? err.error?.title;

    if (err.status === 409) {
      if (this.type() === DeviceType.Thermostat) {
        return `A thermostat already exists in "${this.location().trim()}". Each room can have at most one thermostat.`;
      }
      return problemDetail ?? 'A device with these settings already exists.';
    }

    if (err.status === 400) {
      return problemDetail ?? 'Some fields are invalid. Please check and try again.';
    }

    return problemDetail ?? 'Something went wrong. Please try again.';
  }

  private resetForm(): void {
    this.type.set(null);
    this.name.set('');
    this.location.set('');
    this.errorMessage.set(null);
    this.submitting.set(false);
  }
}
