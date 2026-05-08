/**
 * Maps a raw operation string from the command history audit log into a
 * display category and icon. The categorization is presentational only —
 * the source of truth remains the operation string itself, this just
 * helps the UI render at-a-glance icons.
 *
 * Order of checks matters: scene-decorated operations match the `scene`
 * bucket regardless of their underlying base operation, because the
 * user-meaningful framing of a scene-driven action is "this is part of a
 * scene execution," not "this is a lock toggle."
 */
export type OperationCategory =
  | 'metadata'
  | 'power'
  | 'power-off'
  | 'lock'
  | 'unlock'
  | 'dimmer'
  | 'color'
  | 'speed'
  | 'mode'
  | 'mode-cool'
  | 'mode-heat'
  | 'temperature'
  | 'scene'
  | 'removal'
  | 'other';

export interface OperationClassification {
  category: OperationCategory;
  icon: string;
}

const CATEGORY_ICONS: Record<OperationCategory, string> = {
  metadata: 'pi-pencil',
  power: 'pi-power-off',
  'power-off': 'pi-power-off',
  lock: 'pi-lock',
  unlock: 'pi-lock-open',
  dimmer: 'pi-sun',
  color: 'pi-palette',
  speed: 'pi-forward',
  mode: 'pi-sliders-h',
  'mode-cool': 'pi-asterisk',
  'mode-heat': 'pi-sun',
  temperature: 'pi-chart-line',
  scene: 'pi-play',
  removal: 'pi-trash',
  other: 'pi-circle',
};

export function classifyOperation(operation: string): OperationClassification {
  const op = operation.trim();
  const opLower = op.toLowerCase();

  if (opLower.includes('(scene:') || opLower.startsWith('scene cleanup')) {
    return { category: 'scene', icon: CATEGORY_ICONS.scene };
  }

  if (opLower.startsWith('registered:')) {
    return { category: 'metadata', icon: 'pi-plus-circle' };
  }

  if (opLower.startsWith('updated:')) {
    return { category: 'metadata', icon: CATEGORY_ICONS.metadata };
  }

  if (opLower.startsWith('removed:')) {
    return { category: 'removal', icon: CATEGORY_ICONS.removal };
  }

  if (opLower.startsWith('setpower')) {
    const isOff = /^setpower\s*:\s*off\s*$/i.test(op);
    const category: OperationCategory = isOff ? 'power-off' : 'power';
    return { category, icon: CATEGORY_ICONS[category] };
  }

  if (/^unlock\b/i.test(op)) {
    return { category: 'unlock', icon: CATEGORY_ICONS.unlock };
  }

  if (/^lock\b/i.test(op)) {
    return { category: 'lock', icon: CATEGORY_ICONS.lock };
  }

  if (opLower.startsWith('setbrightness')) {
    return { category: 'dimmer', icon: CATEGORY_ICONS.dimmer };
  }

  if (opLower.startsWith('setcolor')) {
    return { category: 'color', icon: CATEGORY_ICONS.color };
  }

  if (opLower.startsWith('setspeed')) {
    return { category: 'speed', icon: CATEGORY_ICONS.speed };
  }

  if (opLower.startsWith('setmode')) {
    const value = op.match(/^setmode\s*:\s*(\S+)\s*$/i)?.[1]?.toLowerCase();
    let category: OperationCategory = 'mode';
    if (value === 'cool') category = 'mode-cool';
    else if (value === 'heat') category = 'mode-heat';
    return { category, icon: CATEGORY_ICONS[category] };
  }

  if (opLower.startsWith('setdesiredtemperature')) {
    return { category: 'temperature', icon: CATEGORY_ICONS.temperature };
  }

  return { category: 'other', icon: CATEGORY_ICONS.other };
}

/**
 * Translates a raw audit-log operation string into human-readable text.
 *
 * The audit log uses developer-style strings ("SetDesiredTemperature: 72")
 * because they're easy to grep and consistent with the command names. For
 * the History view, we surface them in plain English ("Set temperature to
 * 72°F") so users don't need to know the API to read their own activity.
 *
 * Scene-decorated entries are formatted as "<base action> (from <scene>)"
 * so the scene context is preserved without leaking the parenthesized
 * audit-log syntax.
 */
export function humanizeOperation(operation: string, deviceName?: string): string {
  const op = operation.trim();

  if (/^scene cleanup:/i.test(op)) {
    return 'Cleaned up scene references';
  }

  const sceneMatch = op.match(/^(.+?) \(scene: (.+)\)$/i);
  const baseOp = sceneMatch ? sceneMatch[1] : op;
  const sceneName = sceneMatch ? sceneMatch[2] : null;

  const humanized = humanizeBaseOperation(baseOp, deviceName);

  return sceneName ? `${humanized} (from "${sceneName}")` : humanized;
}

function humanizeBaseOperation(op: string, deviceName?: string): string {
  if (/^updated:/i.test(op)) {
    return op;
  }

  if (/^registered:/i.test(op)) {
    return op;
  }

  if (/^lock\s*:?\s*$/i.test(op)) {
    return deviceName ? `Locked the ${deviceName}` : 'Locked the door';
  }
  if (/^unlock\s*:?\s*$/i.test(op)) {
    return deviceName ? `Unlocked the ${deviceName}` : 'Unlocked the door';
  }

  const power = op.match(/^setpower(?::\s*(.+))?$/i);
  if (power) {
    if (!power[1]) return 'Toggled power';
    return power[1].toLowerCase() === 'on' ? 'Turned on' : 'Turned off';
  }

  const brightness = op.match(/^setbrightness(?::\s*(.+))?$/i);
  if (brightness) {
    return brightness[1] ? `Set brightness to ${brightness[1]}%` : 'Adjusted brightness';
  }

  const color = op.match(/^setcolor(?::\s*(.+))?$/i);
  if (color) {
    return color[1] ? `Set color to ${color[1].toUpperCase()}` : 'Changed color';
  }

  const speed = op.match(/^setspeed(?::\s*(.+))?$/i);
  if (speed) {
    return speed[1] ? `Set speed to ${speed[1]}` : 'Adjusted speed';
  }

  const mode = op.match(/^setmode(?::\s*(.+))?$/i);
  if (mode) {
    return mode[1] ? `Set mode to ${mode[1]}` : 'Changed mode';
  }

  const temp = op.match(/^setdesiredtemperature(?::\s*(.+))?$/i);
  if (temp) {
    return temp[1] ? `Set temperature to ${temp[1]}°F` : 'Adjusted temperature';
  }

  return op;
}

export interface RemovedDeviceMeta {
  name: string;
  type: string;
  location: string;
}

/**
 * Extracts device metadata from a "Removed:" audit-log operation string.
 *
 * The Removed operation embeds name, type, and location at deletion time
 * (e.g. "Removed: Fan 'Bedroom Fan' from 'Bedroom'") so this metadata
 * survives the delete and can be displayed on orphaned history rows.
 *
 * Returns null if the operation isn't a removal or doesn't match the
 * expected shape — callers should fall back to a generic placeholder.
 */
export function parseRemovedDeviceMeta(operation: string): RemovedDeviceMeta | null {
  const match = operation.trim().match(/^Removed:\s+(\S+)\s+'([^']+)'\s+from\s+'([^']+)'$/);
  if (!match) return null;
  return { type: match[1], name: match[2], location: match[3] };
}
