import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChildren,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';

import { AnyDevice } from '../../../device/models/device-types';
import { DeviceType } from '../../../device/models/device';
import { SceneActionRequest, SceneRequest, SceneResponse } from '../../models/scene';
import {
  StagedGroupTarget,
  makeGroupTargetId,
  makeStagedGroupTarget,
} from '../../models/staged-group-target';
import { SceneApiService } from '../../services/scene-api.service';
import { StagedTargetCard } from '../staged-target-card/staged-target-card';
import { StagedGroupCard } from '../staged-group-card/staged-group-card';

const MAX_NAME_LENGTH = 100;

/**
 * Discriminator for the unified ordered list of staged targets.
 * Lets the editor render device cards and group cards interleaved
 * in the order the user added them, rather than splitting them into
 * two visual sections.
 */
type StagedEntry =
  | { kind: 'device'; id: string }
  | { kind: 'group'; id: string };

/**
 * Group option shown in the picker — one row per device type that has
 * at least one matching device and isn't already staged at any-location
 * scope. Location-scoped groups aren't surfaced in the picker; users
 * stage "All X" first then narrow via the location dropdown inside the
 * group card.
 */
interface PickerGroupOption {
  deviceType: DeviceType;
  /** "All Lights", "All Fans", etc. */
  label: string;
  /** Subtitle copy shown below the label. */
  description: string;
  /** Live count of devices currently matching the rule. */
  matchCount: number;
}

/**
 * Modal for creating a new scene OR editing an existing one. The mode
 * is determined by the optional `existingScene` input — when present,
 * the dialog opens pre-populated with the scene's name and staged
 * targets, and Save calls PUT instead of POST.
 *
 * Two kinds of staged targets:
 *
 *   - Device targets: a specific device, configured via StagedTargetCard.
 *     Emits one or more deviceId-bound actions on save.
 *
 *   - Group targets: a (deviceType, location) rule, configured via
 *     StagedGroupCard. Resolves to matching devices at execute time
 *     on the server. Emits one or more deviceType-bound actions on
 *     save with deviceId = null.
 *
 * Both kinds render interleaved in the editor view, in add-order, via
 * a unified `stagedEntries` ordered list. The split into two underlying
 * arrays (stagedDevices and stagedGroups) keeps lookup O(1) and lets
 * each card type bind to its strongly-typed input without ceremony.
 *
 * The picker view exposes both kinds in a single combined list:
 * any-location group rules surface at the top under a GROUPS heading,
 * individual devices below under DEVICES. Filter input matches both
 * sections.
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
    StagedGroupCard,
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
      [header]="dialogHeader()"
      styleClass="aura-dialog"
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

          @if (stagedEntries().length === 0) {
            <div class="scene-editor-targets-placeholder">
              <p>No targets yet. Click "Add target" to choose one.</p>
            </div>
          } @else {
            <div class="scene-editor-targets-list">
              @for (entry of stagedEntries(); track entry.kind + ':' + entry.id) {
                @if (entry.kind === 'device') {
                  @let device = deviceById(entry.id);
                  @if (device !== null) {
                    <aura-staged-target-card
                      [device]="device"
                      [prefilledActions]="actionsByDevice().get(device.id) ?? null"
                      (remove)="onRemoveDevice(device.id)"
                    />
                  }
                } @else {
                  @let group = groupById(entry.id);
                  @if (group !== null) {
                    <aura-staged-group-card
                      [group]="group"
                      [allDevices]="devices()"
                      [prefilledActions]="actionsByGroup().get(group.id) ?? null"
                      (remove)="onRemoveGroup(group.id)"
                      (groupChanged)="onGroupChanged(group.id, $event)"
                    />
                  }
                }
              }
            </div>
          }

          <button
            type="button"
            class="scene-editor-add"
            (click)="onShowPicker()"
          >
            <i class="pi pi-plus"></i>
            Add target
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
              placeholder="Filter…"
              autocomplete="off"
            />
          </div>

          @if (availableGroups().length === 0 && availableDevices().length === 0) {
            <p class="picker-empty">
              @if (pickerFilter().trim().length > 0) {
                No matches for "{{ pickerFilter() }}".
              } @else {
                Everything has been added to this scene.
              }
            </p>
          } @else {
            @if (availableGroups().length > 0) {
              <p class="picker-section-label">Groups</p>
              <ul class="picker-list">
                @for (group of availableGroups(); track group.deviceType) {
                  <li>
                    <button
                      type="button"
                      class="picker-item picker-item-group"
                      (click)="onPickGroupOption(group.deviceType)"
                    >
                      <span class="picker-item-name">{{ group.label }}</span>
                      <span class="picker-item-meta">
                        {{ group.description }} · {{ group.matchCount }}
                        {{ group.matchCount === 1 ? 'device' : 'devices' }}
                      </span>
                    </button>
                  </li>
                }
              </ul>
            }

            @if (availableDevices().length > 0) {
              <p class="picker-section-label">Devices</p>
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
          [label]="saveButtonLabel()"
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
  protected readonly MAX_NAME_LENGTH = MAX_NAME_LENGTH;

  /* ─────────────── Dependencies ─────────────── */

  private readonly sceneApi = inject(SceneApiService);
  private readonly messages = inject(MessageService);

  private readonly stagedDeviceCards = viewChildren(StagedTargetCard);
  private readonly stagedGroupCards = viewChildren(StagedGroupCard);

  /* ─────────────── Inputs / outputs ─────────────── */

  readonly visible = input.required<boolean>();
  readonly devices = input.required<readonly AnyDevice[]>();
  readonly existingScene = input<SceneResponse | null>(null);

  readonly visibleChange = output<boolean>();
  readonly sceneCreated = output<SceneResponse>();
  readonly sceneUpdated = output<SceneResponse>();

  /* ─────────────── Internal state ─────────────── */

  protected readonly view = signal<'editor' | 'picker'>('editor');
  protected readonly name = signal('');
  protected readonly pickerFilter = signal('');
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly stagedDevices = signal<readonly AnyDevice[]>([]);
  protected readonly stagedGroups = signal<readonly StagedGroupTarget[]>([]);
  protected readonly stagedEntries = signal<readonly StagedEntry[]>([]);

  protected readonly actionsByDevice = signal<ReadonlyMap<string, SceneActionRequest[]>>(
    new Map(),
  );
  protected readonly actionsByGroup = signal<ReadonlyMap<string, SceneActionRequest[]>>(
    new Map(),
  );

  /* ─────────────── Mode-aware UI text ─────────────── */

  protected readonly isEditMode = computed(() => this.existingScene() !== null);

  protected readonly dialogHeader = computed(() =>
    this.isEditMode() ? 'Edit Scene' : 'New Scene',
  );

  protected readonly saveButtonLabel = computed(() =>
    this.isEditMode() ? 'Save changes' : 'Save',
  );

  protected readonly canSave = computed(() => {
    const trimmed = this.name().trim();
    const hasName = trimmed.length > 0 && trimmed.length <= MAX_NAME_LENGTH;
    if (!hasName) return false;
    if (this.saving()) return false;

    const deviceCards = this.stagedDeviceCards();
    const groupCards = this.stagedGroupCards();
    if (deviceCards.length === 0 && groupCards.length === 0) return false;

    const anyDeviceTouched = deviceCards.some(c => c.toActions().length > 0);
    const anyGroupTouched = groupCards.some(c => c.toActions().length > 0);
    return anyDeviceTouched || anyGroupTouched;
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

  /**
   * Group rules visible in the picker. One row per device type that
   * has at least one matching device, excluding types already staged
   * at any-location scope. Location-scoped groups are reachable via
   * the dropdown inside a staged group card, not through the picker.
   *
   * Filter applies to the label ("All Lights") and description
   * ("Targets every light"), matching the same case-insensitive rules
   * used for individual devices.
   */
  protected readonly availableGroups = computed<PickerGroupOption[]>(() => {
    const allDevices = this.devices();
    const stagedAnyLocationTypes = new Set(
      this.stagedGroups()
        .filter(g => g.location === null)
        .map(g => g.deviceType),
    );

    // Count devices by type
    const countsByType = new Map<DeviceType, number>();
    for (const d of allDevices) {
      countsByType.set(d.type, (countsByType.get(d.type) ?? 0) + 1);
    }

    const query = this.pickerFilter().trim().toLowerCase();
    const options: PickerGroupOption[] = [];

    // Iterate the DeviceType enum in a stable order. The values of
    // a string enum aren't iterable like a numeric enum, so we
    // enumerate explicitly. Order here is the picker's display order.
    const orderedTypes: DeviceType[] = [
      DeviceType.Light,
      DeviceType.Fan,
      DeviceType.Thermostat,
      DeviceType.DoorLock,
    ];

    for (const type of orderedTypes) {
      const count = countsByType.get(type) ?? 0;
      if (count === 0) continue;
      if (stagedAnyLocationTypes.has(type)) continue;

      const label = this.groupLabelForType(type);
      const description = this.groupDescriptionForType(type);

      if (query.length > 0) {
        const matches =
          label.toLowerCase().includes(query) ||
          description.toLowerCase().includes(query);
        if (!matches) continue;
      }

      options.push({ deviceType: type, label, description, matchCount: count });
    }

    return options;
  });

  /* ─────────────── Lifecycle / mode-switching ─────────────── */

  constructor() {
    /**
     * When the dialog opens (visible flips to true), check for edit
     * mode and seed state from the existing scene. The `untracked`
     * read ensures we don't subscribe to our own writes — the effect
     * should only run when `visible` or `existingScene` changes from
     * outside.
     */
    effect(() => {
      const open = this.visible();
      const scene = this.existingScene();

      untracked(() => {
        if (open && scene !== null) {
          this.seedFromExistingScene(scene);
        } else if (!open) {
          this.actionsByDevice.set(new Map());
          this.actionsByGroup.set(new Map());
        }
      });
    });
  }

  /* ─────────────── Template lookup helpers ─────────────── */

  protected deviceById(id: string): AnyDevice | null {
    return this.stagedDevices().find(d => d.id === id) ?? null;
  }

  protected groupById(id: string): StagedGroupTarget | null {
    return this.stagedGroups().find(g => g.id === id) ?? null;
  }

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
    this.stagedEntries.update(list => [...list, { kind: 'device', id: device.id }]);
    this.view.set('editor');
    this.pickerFilter.set('');
  }

  /**
   * Picker handler for a group option. Always stages at any-location
   * scope; the user can narrow via the dropdown inside the staged
   * group card. Delegates to onSelectGroup which handles dedup.
   */
  protected onPickGroupOption(deviceType: DeviceType): void {
    this.onSelectGroup(deviceType, null);
  }

  /**
   * Public entry for adding a group target. Wires dedup at the data
   * layer regardless of how the target was selected (picker, programmatic
   * test, etc.). Returns true if added, false if already staged.
   */
  onSelectGroup(deviceType: DeviceType, location: string | null): boolean {
    const target = makeStagedGroupTarget(deviceType, location);
    const existing = this.stagedGroups().find(g => g.id === target.id);
    if (existing !== undefined) {
      this.messages.add({
        severity: 'info',
        summary: 'Already added',
        detail: 'That group target is already in this scene.',
        life: 2500,
      });
      return false;
    }

    this.stagedGroups.update(list => [...list, target]);
    this.stagedEntries.update(list => [...list, { kind: 'group', id: target.id }]);
    this.view.set('editor');
    this.pickerFilter.set('');
    return true;
  }

  protected onRemoveDevice(id: string): void {
    this.stagedDevices.update(list => list.filter(d => d.id !== id));
    this.stagedEntries.update(list =>
      list.filter(e => !(e.kind === 'device' && e.id === id)));
    this.actionsByDevice.update(map => {
      if (!map.has(id)) return map;
      const next = new Map(map);
      next.delete(id);
      return next;
    });
  }

  protected onRemoveGroup(id: string): void {
    this.stagedGroups.update(list => list.filter(g => g.id !== id));
    this.stagedEntries.update(list =>
      list.filter(e => !(e.kind === 'group' && e.id === id)));
    this.actionsByGroup.update(map => {
      if (!map.has(id)) return map;
      const next = new Map(map);
      next.delete(id);
      return next;
    });
  }

  /**
   * Handles a group's location-scope change. The new target has a
   * different composite id, so we swap it in place in stagedGroups
   * and stagedEntries (preserving order). If the new id collides with
   * another already-staged group, the change is rejected with a toast
   * and the array identity is toggled to bounce the input back to the
   * card, reverting its location dropdown to the previous value.
   */
  protected onGroupChanged(oldId: string, replacement: StagedGroupTarget): void {
    if (replacement.id === oldId) return;

    const groups = this.stagedGroups();
    const collides = groups.some(g => g.id === replacement.id);
    if (collides) {
      this.messages.add({
        severity: 'warn',
        summary: 'Group already exists',
        detail: 'That location is already covered by another group target.',
        life: 3000,
      });
      this.stagedGroups.update(list => [...list]);
      return;
    }

    this.stagedGroups.update(list =>
      list.map(g => (g.id === oldId ? replacement : g)));
    this.stagedEntries.update(list =>
      list.map(e =>
        e.kind === 'group' && e.id === oldId
          ? { kind: 'group', id: replacement.id }
          : e));

    this.actionsByGroup.update(map => {
      if (!map.has(oldId)) return map;
      const updated = new Map(map);
      const actions = updated.get(oldId)!;
      updated.delete(oldId);
      updated.set(replacement.id, actions);
      return updated;
    });
  }

  protected onCancel(): void {
    this.visibleChange.emit(false);
    this.resetState();
  }

  protected onSave(): void {
    const deviceCards = this.stagedDeviceCards();
    const groupCards = this.stagedGroupCards();

    const deviceActions = deviceCards.flatMap(c => c.toActions());
    const groupActions = groupCards.flatMap(c => c.toActions());
    const actions = [...deviceActions, ...groupActions];

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

    const existing = this.existingScene();
    if (existing !== null) {
      this.sceneApi.update(existing.id, request).subscribe({
        next: (scene) => {
          this.sceneUpdated.emit(scene);
          this.visibleChange.emit(false);
          this.resetState();
        },
        error: (err: HttpErrorResponse) => {
          this.saving.set(false);
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
    } else {
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
  }

  /* ─────────────── Edit-mode seeding ─────────────── */

  /**
   * Pre-populate name, staged targets, and per-target action maps from
   * an existing scene. Walks the scene's actions once, separating into
   * device-keyed and group-keyed buckets:
   *
   *   - Actions with deviceId set → grouped under that deviceId, become
   *     a StagedTargetCard.
   *   - Actions with deviceType set (and deviceId null) → grouped by
   *     composite group id, become a StagedGroupCard.
   *
   * Add-order in stagedEntries follows the order each target is first
   * encountered in scene.actions, which mirrors the natural reading
   * order on the server.
   */
  private seedFromExistingScene(scene: SceneResponse): void {
    const allDevices = this.devices();
    const deviceById = new Map(allDevices.map(d => [d.id, d] as const));

    const actionsByDevice = new Map<string, SceneActionRequest[]>();
    const actionsByGroup = new Map<string, SceneActionRequest[]>();
    const stagedDevices: AnyDevice[] = [];
    const stagedGroups: StagedGroupTarget[] = [];
    const entries: StagedEntry[] = [];

    for (const action of scene.actions) {
      if (action.deviceId != null) {
        if (!deviceById.has(action.deviceId)) continue;
        const id = action.deviceId;
        if (!actionsByDevice.has(id)) {
          actionsByDevice.set(id, []);
          stagedDevices.push(deviceById.get(id)!);
          entries.push({ kind: 'device', id });
        }
        actionsByDevice.get(id)!.push(action);
      } else if (action.deviceType != null) {
        const groupId = makeGroupTargetId(
          action.deviceType,
          action.location ?? null,
        );
        if (!actionsByGroup.has(groupId)) {
          actionsByGroup.set(groupId, []);
          stagedGroups.push(
            makeStagedGroupTarget(
              action.deviceType,
              action.location ?? null,
            ),
          );
          entries.push({ kind: 'group', id: groupId });
        }
        actionsByGroup.get(groupId)!.push(action);
      }
    }

    this.name.set(scene.name);
    this.stagedDevices.set(stagedDevices);
    this.stagedGroups.set(stagedGroups);
    this.stagedEntries.set(entries);
    this.actionsByDevice.set(actionsByDevice);
    this.actionsByGroup.set(actionsByGroup);
    this.view.set('editor');
    this.pickerFilter.set('');
    this.errorMessage.set(null);
  }

  /* ─────────────── Display helpers ─────────────── */

  private groupLabelForType(type: DeviceType): string {
    switch (type) {
      case DeviceType.Light: return 'All Lights';
      case DeviceType.Fan: return 'All Fans';
      case DeviceType.Thermostat: return 'All Thermostats';
      case DeviceType.DoorLock: return 'All Door Locks';
    }
  }

  private groupDescriptionForType(type: DeviceType): string {
    switch (type) {
      case DeviceType.Light: return 'Targets every light';
      case DeviceType.Fan: return 'Targets every fan';
      case DeviceType.Thermostat: return 'Targets every thermostat';
      case DeviceType.DoorLock: return 'Targets every door lock';
    }
  }

  /* ─────────────── Helpers ─────────────── */

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

  private resetState(): void {
    this.view.set('editor');
    this.name.set('');
    this.stagedDevices.set([]);
    this.stagedGroups.set([]);
    this.stagedEntries.set([]);
    this.actionsByDevice.set(new Map());
    this.actionsByGroup.set(new Map());
    this.pickerFilter.set('');
    this.saving.set(false);
    this.errorMessage.set(null);
  }
}
