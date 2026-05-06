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
  | 'lock'
  | 'dimmer'
  | 'color'
  | 'speed'
  | 'mode'
  | 'temperature'
  | 'scene'
  | 'other';

export interface OperationClassification {
  category: OperationCategory;
  icon: string;
}

const CATEGORY_ICONS: Record<OperationCategory, string> = {
  metadata: 'pi-pencil',
  power: 'pi-power-off',
  lock: 'pi-lock',
  dimmer: 'pi-sun',
  color: 'pi-palette',
  speed: 'pi-forward',
  mode: 'pi-sliders-h',
  temperature: 'pi-chart-line',
  scene: 'pi-play',
  other: 'pi-circle',
};

export function classifyOperation(operation: string): OperationClassification {
  const op = operation.trim();

  if (op.includes('(scene:') || op.startsWith('Scene cleanup')) {
    return {category: 'scene', icon: CATEGORY_ICONS.scene};
  }

  if (op.startsWith('Updated:')) {
    return {category: 'metadata', icon: CATEGORY_ICONS.metadata};
  }

  if (op.startsWith('SetPower')) {
    return {category: 'power', icon: CATEGORY_ICONS.power};
  }

  if (op.startsWith('Lock') || op.startsWith('Unlock')) {
    return {category: 'lock', icon: CATEGORY_ICONS.lock};
  }

  if (op.startsWith('SetBrightness')) {
    return {category: 'dimmer', icon: CATEGORY_ICONS.dimmer};
  }

  if (op.startsWith('SetColor')) {
    return {category: 'color', icon: CATEGORY_ICONS.color};
  }

  if (op.startsWith('SetSpeed')) {
    return {category: 'speed', icon: CATEGORY_ICONS.speed};
  }

  if (op.startsWith('SetMode')) {
    return {category: 'mode', icon: CATEGORY_ICONS.mode};
  }

  if (op.startsWith('SetDesiredTemperature')) {
    return {category: 'temperature', icon: CATEGORY_ICONS.temperature};
  }

  return {category: 'other', icon: CATEGORY_ICONS.other};
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
export function humanizeOperation(operation: string): string {
  const op = operation.trim();

  // Scene cleanup: terse, just describe what happened.
  if (op.startsWith('Scene cleanup:')) {
    return 'Cleaned up scene references';
  }

  // Detect and strip scene suffix so we humanize the base operation,
  // then re-attach the scene name afterward.
  const sceneMatch = op.match(/^(.+?) \(scene: (.+)\)$/);
  const baseOp = sceneMatch ? sceneMatch[1] : op;
  const sceneName = sceneMatch ? sceneMatch[2] : null;

  const humanized = humanizeBaseOperation(baseOp);

  return sceneName ? `${humanized} (from "${sceneName}")` : humanized;
}

function humanizeBaseOperation(op: string): string {
  // Updated: name '...' → '...', location '...' → '...'
  // Pass through unchanged — the format is already readable.
  if (op.startsWith('Updated:')) {
    return op;
  }

  // Lock / Unlock — present tense, no value.
  if (op === 'Lock') return 'Locked';
  if (op === 'Unlock') return 'Unlocked';

  // SetPower: On / Off (or just SetPower for scene-driven commands)
  const power = op.match(/^SetPower(?::\s*(.+))?$/);
  if (power) {
    if (!power[1]) return 'Toggled power';
    return power[1].toLowerCase() === 'on' ? 'Turned on' : 'Turned off';
  }

  // SetBrightness: 80 (or just SetBrightness)
  const brightness = op.match(/^SetBrightness(?::\s*(.+))?$/);
  if (brightness) {
    return brightness[1] ? `Set brightness to ${brightness[1]}%` : 'Adjusted brightness';
  }

  // SetColor: #FF8800 (or just SetColor)
  const color = op.match(/^SetColor(?::\s*(.+))?$/);
  if (color) {
    return color[1] ? `Set color to ${color[1].toUpperCase()}` : 'Changed color';
  }

  // SetSpeed: Medium (or just SetSpeed)
  const speed = op.match(/^SetSpeed(?::\s*(.+))?$/);
  if (speed) {
    return speed[1] ? `Set speed to ${speed[1]}` : 'Adjusted speed';
  }

  // SetMode: Cool (or just SetMode)
  const mode = op.match(/^SetMode(?::\s*(.+))?$/);
  if (mode) {
    return mode[1] ? `Set mode to ${mode[1]}` : 'Changed mode';
  }

  // SetDesiredTemperature: 72 (or just SetDesiredTemperature)
  const temp = op.match(/^SetDesiredTemperature(?::\s*(.+))?$/);
  if (temp) {
    return temp[1] ? `Set temperature to ${temp[1]}°F` : 'Adjusted temperature';
  }

  // Unrecognized — pass through as-is rather than swallow the data.
  return op;
}
