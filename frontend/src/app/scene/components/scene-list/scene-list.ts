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
import { StagedTargetCard } from '../staged-target-card/staged-target-card';
import { SceneEditorDialog } from '../scene-editor-dialog/scene-editor-dialog';
/**
 * The /scenes route. Owns the live device list (kept current via SSE),
 * the scenes list, and the two execution dialogs. Hands devices down
 * to scene cards (recipe display) and to the execution dialog (paced
 * playback). Both consumers see the same signal — when SSE delivers
 * an event, the recipe re-resolves and the dialog's leaf visual
 * transitions, in lockstep with the dashboard.
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
    StagedTargetCard,
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
          (onClick)="editorDialogVisible.set(true)"
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
              (sceneRemoved)="onSceneRemoved($event)"
            />
          }
        </div>
      }

      <p-confirmpopup />

      <aura-scene-editor-dialog
        [visible]="editorDialogVisible()"
        [devices]="devices()"
        (visibleChange)="editorDialogVisible.set($event)"
        (sceneCreated)="onSceneCreated($event)"
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

  onSceneCreated(scene: SceneResponse): void {
    this.scenes.update((current) => [...current, scene]);
  }

  onSceneRemoved(id: string): void {
    this.scenes.update((current) => current.filter((s) => s.id !== id));
  }

  onSandboxRemove(): void {
    console.log('[sandbox] StagedTargetCard remove clicked');
  }

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
