import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { ConfirmPopupModule } from 'primeng/confirmpopup';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';

import { SceneApiService } from '../../services/scene-api.service';
import {
  SceneExecutionResponse,
  SceneResponse,
} from '../../models/scene';
import { DeviceApiService } from '../../../device/services/device-api.service';
import { DeviceEventService } from '../../../device/services/device-event.service';
import { AnyDevice } from '../../../device/models/device-types';
import { DeviceChangeType, DeviceChangedEvent } from '../../../device/models/device';
import { RecipeStep } from '../../services/scene-recipe';
import { SceneCard } from '../scene-card/scene-card';
import { SceneExecutionDialog } from '../scene-execution-dialog/scene-execution-dialog';
import { SceneExecutionResultDialog } from '../scene-execution-result-dialog/scene-execution-result-dialog';
import { SceneEditorDialog } from '../scene-editor-dialog/scene-editor-dialog';

/**
 * The /scenes route. Owns the live device list (kept current via SSE),
 * the scenes list, the editor dialog (used for both create and edit),
 * and the two execution dialogs. Hands devices down to scene cards
 * (recipe display) and to the execution dialog (paced playback). Both
 * consumers see the same signal — when SSE delivers an event, the
 * recipe re-resolves and the dialog's leaf visual transitions, in
 * lockstep with the dashboard.
 *
 * Edit and Delete are surfaced from each scene card as outputs that
 * the list handles. Edit opens the editor dialog with the scene
 * pre-loaded; Delete confirms via PrimeNG's ConfirmationService and
 * fires the API call on confirm.
 *
 * The result dialog only auto-opens on partial failure; on full success
 * the playback dialog is the user-facing confirmation.
 */
@Component({
  selector: 'aura-scene-list',
  standalone: true,
  imports: [
    ButtonModule,
    ConfirmPopupModule,
    TooltipModule,
    SceneCard,
    SceneExecutionDialog,
    SceneExecutionResultDialog,
    SceneEditorDialog,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="scene-list">
      <header class="scene-list-header">
        <div class="scene-list-titles">
          <h1>Scenes</h1>
          @if (scenes().length > 0) {
            <p class="scene-list-summary">
              {{ scenes().length }}
              {{ scenes().length === 1 ? 'scene' : 'scenes' }}
            </p>
          }
        </div>
        <p-button
          label="New Scene"
          icon="pi pi-plus"
          (onClick)="onNewSceneClick()"
        />
      </header>

      @if (loading()) {
        <p class="scene-list-status">Loading scenes…</p>
      } @else if (error()) {
        <p class="scene-list-status error">{{ error() }}</p>
      } @else if (scenes().length === 0) {
        <p class="scene-list-status muted">
          No scenes yet. Scenes let you run multiple device commands at
          once — like "Goodnight" turning off all lights and locking the
          doors.
        </p>
      } @else {
        <div class="scene-list-grid">
          @for (scene of scenes(); track scene.id) {
            <aura-scene-card
              [scene]="scene"
              [allDevices]="devices()"
              (sceneExecuted)="onSceneExecuted($event)"
              (editRequested)="onEditRequested($event)"
              (deleteRequested)="onDeleteRequested($event)"
            />
          }
        </div>
      }

      <aura-scene-editor-dialog
        [visible]="editorDialogVisible()"
        [devices]="devices()"
        [existingScene]="editingScene()"
        (visibleChange)="onEditorVisibleChange($event)"
        (sceneCreated)="onSceneCreated($event)"
        (sceneUpdated)="onSceneUpdated($event)"
      />

      <aura-scene-execution-dialog
        [visible]="executionDialogVisible()"
        [sceneName]="lastExecutedSceneName()"
        [steps]="executionRecipe()"
        [allDevices]="devices()"
        [beforeDevices]="executionBeforeDevices()"
        (visibleChange)="executionDialogVisible.set($event)"
      />

      <aura-scene-execution-result-dialog
        [visible]="resultDialogVisible()"
        [result]="lastExecutionResult()"
        (visibleChange)="resultDialogVisible.set($event)"
      />
    </section>
  `,
  styleUrl: './scene-list.scss',
})
export class SceneList implements OnInit, OnDestroy {
  private readonly sceneApi = inject(SceneApiService);
  private readonly deviceApi = inject(DeviceApiService);
  private readonly events = inject(DeviceEventService);
  private readonly confirms = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  readonly scenes = signal<SceneResponse[]>([]);
  readonly devices = signal<AnyDevice[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly executionDialogVisible = signal(false);
  readonly executionRecipe = signal<RecipeStep[]>([]);
  readonly executionBeforeDevices = signal<readonly AnyDevice[]>([]);
  readonly lastExecutedSceneName = signal('');

  readonly resultDialogVisible = signal(false);
  readonly lastExecutionResult = signal<SceneExecutionResponse | null>(null);

  readonly editorDialogVisible = signal(false);

  /**
   * The scene being edited, or null when the editor is in create mode.
   * Set by onEditRequested before opening the dialog; cleared when the
   * dialog closes (via onEditorVisibleChange) so the next New Scene
   * click opens in create mode regardless of the previous session.
   */
  readonly editingScene = signal<SceneResponse | null>(null);

  constructor() {
    this.events.events$
      .pipe(takeUntilDestroyed())
      .subscribe(evt => this.applyEvent(evt));
  }

  ngOnInit(): void {
    forkJoin({
      scenes: this.sceneApi.getAll(),
      devices: this.deviceApi.getAll(),
    }).subscribe({
      next: ({ scenes, devices }) => {
        this.scenes.set(scenes);
        this.devices.set(devices);
        this.loading.set(false);
        this.events.connect();
      },
      error: (err) => {
        this.error.set('Could not load scenes. Is the API running?');
        this.loading.set(false);
        console.error(err);
      },
    });
  }

  ngOnDestroy(): void {
    this.events.disconnect();
  }

  /* ─────────────── Editor dialog wiring ─────────────── */

  onNewSceneClick(): void {
    // Ensure create mode — clear any stale editingScene from a prior
    // edit session before opening.
    this.editingScene.set(null);
    this.editorDialogVisible.set(true);
  }

  onEditRequested(scene: SceneResponse): void {
    this.editingScene.set(scene);
    this.editorDialogVisible.set(true);
  }

  onEditorVisibleChange(open: boolean): void {
    this.editorDialogVisible.set(open);
    if (!open) {
      // Always clear editingScene when the dialog closes so that the
      // next New Scene click is unambiguously create mode.
      this.editingScene.set(null);
    }
  }

  onSceneCreated(scene: SceneResponse): void {
    this.scenes.update(current => [...current, scene]);
  }

  onSceneUpdated(scene: SceneResponse): void {
    this.scenes.update(current =>
      current.map(s => (s.id === scene.id ? scene : s)));
    this.messages.add({
      severity: 'success',
      summary: 'Scene updated',
      detail: `"${scene.name}" was saved.`,
      life: 2500,
    });
  }

  /* ─────────────── Delete flow ─────────────── */

  onDeleteRequested(scene: SceneResponse): void {
    this.confirms.confirm({
      header: 'Delete scene',
      message: `Delete "${scene.name}"? This cannot be undone.`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => this.executeDelete(scene),
    });
  }

  private executeDelete(scene: SceneResponse): void {
    this.sceneApi.remove(scene.id).subscribe({
      next: () => {
        this.scenes.update(current => current.filter(s => s.id !== scene.id));
        this.messages.add({
          severity: 'success',
          summary: 'Scene deleted',
          detail: `"${scene.name}" was removed.`,
          life: 2500,
        });
      },
      error: (err: HttpErrorResponse) => {
        this.messages.add({
          severity: 'error',
          summary: 'Delete failed',
          detail:
            err.error?.detail ??
            err.error?.title ??
            'Could not delete scene. Please try again.',
          life: 4000,
        });
      },
    });
  }

  /* ─────────────── Execution wiring ─────────────── */

  onSceneExecuted(emitted: {
    result: SceneExecutionResponse;
    recipe: RecipeStep[];
    beforeDevices: readonly AnyDevice[];
  }): void {
    this.lastExecutionResult.set(emitted.result);
    this.lastExecutedSceneName.set(emitted.result.sceneName);

    if (emitted.recipe.length > 0) {
      this.executionRecipe.set(emitted.recipe);
      this.executionBeforeDevices.set(emitted.beforeDevices);
      this.executionDialogVisible.set(true);
    }

    if (emitted.result.summary.failed > 0) {
      this.resultDialogVisible.set(true);
    }
  }

  /* ─────────────── SSE event handling ─────────────── */

  private applyEvent(evt: DeviceChangedEvent): void {
    switch (evt.changeType) {
      case DeviceChangeType.Created:
        this.devices.update(current => {
          const incoming = evt.payload as unknown as AnyDevice;
          return current.some(d => d.id === incoming.id)
            ? current
            : [...current, incoming];
        });
        break;

      case DeviceChangeType.Updated:
        this.devices.update(current =>
          current.map(d =>
            d.id === evt.deviceId
              ? (evt.payload as unknown as AnyDevice)
              : d));
        break;

      case DeviceChangeType.Deleted:
        this.devices.update(current =>
          current.filter(d => d.id !== evt.deviceId));
        break;
    }
  }
}
