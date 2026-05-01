import { DeviceType } from '../../device/models/device';
import {
  AnyDevice,
  isDoorLock,
  isFan,
  isLight,
  isThermostat,
} from '../../device/models/device-types';
import { SceneActionResponse, SceneResponse } from '../models/scene';

/**
 * One row in a scene's recipe. Drives both the static recipe list in the
 * scene card and the paced playback in the execution dialog. The popup
 * looks up its current step's deviceId in the live device list on every
 * render — when SSE patches the device list, the leaf visual rebinds and
 * transitions naturally.
 */
export interface RecipeStep {
  /** 1-indexed display number. Matches what the user sees in the recipe. */
  ordinal: number;
  /** "Turn on Living Room Fan", "Dim Porch Light to 20%", etc. */
  label: string;
  /** Right-column type label: "Light", "Fan", "Thermostat", "Door Lock". */
  typeLabel: string;
  /** Resolved device id. Empty when the action's target no longer exists. */
  deviceId: string;
  /**
   * Resolved device at recipe-build time. Treat this as a starting hint —
   * the popup re-resolves on every render against the live device list,
   * so SSE-driven updates propagate without rebuilding the recipe.
   */
  device: AnyDevice | null;
}

/**
 * Build a recipe from a scene against the current device registry.
 *
 * Steps are emitted in orderIndex order. Group targets ("all lights in
 * Kitchen") expand one step per resolved device, matching what executes.
 * Group targets that resolve to zero devices contribute nothing — no
 * "ghost step" for an empty group. Specific-device targets that point
 * at a removed device emit a step with deviceId = '' so the recipe can
 * render a greyed-out "missing device" row.
 */
export function buildRecipe(
  scene: SceneResponse,
  allDevices: readonly AnyDevice[],
): RecipeStep[] {
  const ordered = [...scene.actions].sort((a, b) => a.orderIndex - b.orderIndex);
  const steps: RecipeStep[] = [];

  for (const action of ordered) {
    for (const resolution of resolveStepTargets(action, allDevices)) {
      steps.push({
        ordinal: steps.length + 1,
        label: describeAction(action, resolution.deviceName),
        typeLabel: typeLabelFor(action, resolution.device),
        deviceId: resolution.device?.id ?? '',
        device: resolution.device,
      });
    }
  }

  return steps;
}

/* ─────────────── Target resolution ─────────────── */

interface StepTarget {
  device: AnyDevice | null;
  deviceName: string;
}

/**
 * Expand an action into one or more concrete step targets. A specific-
 * device target yields one entry (with device: null if missing). A group
 * target yields one entry per matching device, or nothing if none match.
 */
function resolveStepTargets(
  action: SceneActionResponse,
  devices: readonly AnyDevice[],
): StepTarget[] {
  if (action.deviceId) {
    const found = devices.find(d => d.id === action.deviceId);
    return [{
      device: found ?? null,
      deviceName: found?.name ?? 'missing device',
    }];
  }

  if (action.deviceType != null) {
    return devices
      .filter(d => d.type === action.deviceType &&
        (!action.location || d.location === action.location))
      .map(device => ({ device, deviceName: device.name }));
  }

  return [];
}

/* ─────────────── Phrasing ─────────────── */

/**
 * Operation-aware phrasing. Reads each operation's value the way the
 * operation expects it: SetBrightness as a percent, SetColor as a hex,
 * SetSpeed/SetMode as enum strings, SetDesiredTemperature as °F.
 */
function describeAction(action: SceneActionResponse, deviceName: string): string {
  switch (action.operation) {
    case 'SetPower':
      return action.value === 'On'
        ? `Turn on ${deviceName}`
        : `Turn off ${deviceName}`;

    case 'SetBrightness':
      return `Set ${deviceName} brightness to ${action.value ?? '?'}%`;

    case 'SetColor':
      return `Set ${deviceName} color to ${(action.value ?? '').toUpperCase()}`;

    case 'SetSpeed':
      return `Set ${deviceName} speed to ${action.value ?? '?'}`;

    case 'SetMode':
      return `Set ${deviceName} mode to ${action.value ?? '?'}`;

    case 'SetDesiredTemperature':
      return `Set ${deviceName} target to ${action.value ?? '?'}°F`;

    case 'Lock':
      return `Lock ${deviceName}`;

    case 'Unlock':
      return `Unlock ${deviceName}`;

    default:
      // Fallback for unknown operations: show the operation name verbatim
      // so a stale or future-shaped scene is still readable.
      return `${action.operation} ${deviceName}`;
  }
}


function typeLabelFor(action: SceneActionResponse, device: AnyDevice | null): string {
  const type = device?.type ?? action.deviceType;
  switch (type) {
    case DeviceType.Light: return 'Light';
    case DeviceType.Fan: return 'Fan';
    case DeviceType.Thermostat: return 'Thermostat';
    case DeviceType.DoorLock: return 'Door Lock';
    default: return '';
  }
}
