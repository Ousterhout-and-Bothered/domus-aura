import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { HttpErrorResponse } from '@angular/common/http';

import { SceneApiService } from '../../services/scene-api.service';
import {
  SceneExecutionResponse,
  SceneResponse,
} from '../../models/scene';
import { AnyDevice } from '../../../device/models/device-types';
import { RecipeStep, buildRecipe } from '../../services/scene-recipe';


/**
 * One row in the scene list. Renders the scene name, a brief summary,
 * Execute and Remove buttons, and an expandable recipe panel showing
 * the numbered list of actions the scene will perform.
 *
 * Execute fires the API call and emits a sceneExecuted event with the
 * recipe attached. The parent (SceneList) owns the execution dialog —
 * we don't open a modal from here, because only the parent has the
 * live device list signal that drives the dialog's animations.
 */
@Component({
  selector: 'aura-scene-card',
  standalone: true,
  imports: [ButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.is-expanded]': 'expanded()',
  },
  template: `
    <article class="scene-card">
      <button
        type="button"
        class="scene-card-remove"
        [attr.aria-label]="'Remove ' + scene().name"
        [disabled]="executing()"
        (click)="onRequestRemove($event)"
      >
        <i class="pi pi-times"></i>
      </button>

      <header class="scene-card-head">
        <div class="scene-card-titles">
          <h2 class="scene-card-name">{{ scene().name }}</h2>
          <p class="scene-card-summary">{{ summary() }}</p>
        </div>
      </header>

      <div class="scene-card-actions">
        <button
          type="button"
          class="scene-card-chevron"
          [class.is-expanded]="expanded()"
          [attr.aria-label]="(expanded() ? 'Collapse' : 'Expand') + ' ' + scene().name"
          [attr.aria-expanded]="expanded()"
          (click)="onToggleExpanded()"
        >
          <i class="pi pi-chevron-down"></i>
        </button>
        <p-button
          label="Execute"
          icon="pi pi-play"
          severity="primary"
          [loading]="executing()"
          [disabled]="executing()"
          (onClick)="onExecute()"
        />
      </div>

      @if (expanded()) {
        <div class="scene-card-recipe">
          @if (recipe().length === 0) {
            <p class="scene-card-empty">
              No matching devices for this scene's targets.
            </p>
          } @else {
            <ol class="recipe-list">
              @for (step of recipe(); track step.ordinal) {
                <li
                  class="recipe-row"
                  [class.is-missing]="!step.deviceId"
                >
                  <span class="recipe-num">{{ step.ordinal }}.</span>
                  <span class="recipe-label">
                {{ step.label }}
              </span>
                  <span class="recipe-type">{{ step.typeLabel }}</span>
                </li>
              }
            </ol>
          }
        </div>
      }
    </article>
  `,
  styleUrl: './scene-card.scss',
})
export class SceneCard {
  private readonly api = inject(SceneApiService);
  private readonly messages = inject(MessageService);
  private readonly confirms = inject(ConfirmationService);

  readonly scene = input.required<SceneResponse>();
  readonly allDevices = input<readonly AnyDevice[]>([]);


  /** Fired after execute resolves. Parent opens the execution dialog with the recipe. */
  readonly sceneExecuted = output<{
    result: SceneExecutionResponse;
    recipe: RecipeStep[];
    beforeDevices: readonly AnyDevice[];
  }>();

  /** Fired after a successful remove. */
  readonly sceneRemoved = output<string>();

  readonly executing = signal(false);
  readonly expanded = signal(false);

  readonly summary = computed(() => {
    const actions = this.scene().actions;
    if (actions.length === 0) return 'No actions';

    const targetTypes = new Set<string>();
    for (const a of actions) {
      if (a.deviceType != null) {
        targetTypes.add(a.deviceType.toString().toLowerCase() + 's');
      } else if (a.deviceId != null) {
        targetTypes.add('specific devices');
      }
    }

    const actionWord = actions.length === 1 ? 'action' : 'actions';
    const targetList = Array.from(targetTypes).join(', ');

    return targetList
      ? `${actions.length} ${actionWord} · targets ${targetList}`
      : `${actions.length} ${actionWord}`;
  });

  /**
   * The recipe rebuilds whenever scene or allDevices changes — including
   * SSE-driven updates. A renamed device shows the new name in the
   * recipe immediately; a deleted device shows as missing.
   */
  readonly recipe = computed(() =>
    buildRecipe(this.scene(), this.allDevices()));

  onToggleExpanded(): void {
    this.expanded.update(v => !v);
  }

  onExecute(): void {
    if (this.executing()) return;
    this.executing.set(true);

    // Capture the recipe *before* the API call. The execution dialog
    // uses these steps to drive playback; if we built it after, a
    // rapid-fire rename or delete arriving via SSE could shift the
    // step list under the dialog while it plays.
    const recipe = this.recipe();
    const beforeDevices = this.allDevices().map(d => structuredClone(d));

    this.api.execute(this.scene().id).subscribe({
      next: (result) => {
        this.executing.set(false);
        this.sceneExecuted.emit({ result, recipe, beforeDevices });

        if (result.summary.failed > 0) {
          this.messages.add({
            severity: 'warn',
            summary: 'Scene partially executed',
            detail: `${result.summary.failed} of ${
              result.summary.failed + result.summary.succeeded
            } actions failed`,
          });
        } else {
          this.messages.add({
            severity: 'success',
            summary: 'Scene executed',
            detail: `${result.summary.succeeded} actions completed`,
          });
        }
      },
      error: (err: HttpErrorResponse) => {
        this.executing.set(false);
        this.messages.add({
          severity: 'error',
          summary: 'Execution failed',
          detail:
            err.error?.detail ??
            err.error?.title ??
            'Could not execute scene. Please try again.',
        });
      },
    });
  }

  onRequestRemove(event: MouseEvent): void {
    this.confirms.confirm({
      target: event.currentTarget as HTMLElement,
      header: 'Remove scene',
      message: `Remove "${this.scene().name}"? This cannot be undone.`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Remove',
      acceptButtonStyleClass: 'p-button-danger',
      rejectLabel: 'Cancel',
      accept: () => this.executeRemove(),
    });
  }

  private executeRemove(): void {
    const id = this.scene().id;
    const name = this.scene().name;

    this.api.remove(id).subscribe({
      next: () => {
        this.sceneRemoved.emit(id);
        this.messages.add({
          severity: 'success',
          summary: 'Scene removed',
          detail: `"${name}" was deleted.`,
        });
      },
      error: (err: HttpErrorResponse) => {
        this.messages.add({
          severity: 'error',
          summary: 'Remove failed',
          detail:
            err.error?.detail ??
            err.error?.title ??
            'Could not remove scene. Please try again.',
        });
      },
    });
  }
}
