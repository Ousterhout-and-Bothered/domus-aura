import { AnyDevice } from '../models/device-types';
import { DeviceType, PowerState } from '../models/device';
import { ThermostatState } from '../models/device-types';

/**
 * Filter state for the device dashboard. All fields are independent
 * and combinable */

export interface DeviceFilters {
  readonly powerStatus: 'all' | 'on' | 'off';
  readonly location: string | null;
  readonly types: ReadonlySet<DeviceType>;
}

export const DEFAULT_FILTERS: DeviceFilters = {
  powerStatus: 'all',
  location: null,
  types: new Set(),
};

/**
 * Thermostat is "on" only when actively heating or cooling. Idle is technically
 * powered but not doing work; the rubric language ("active") supports either
 * interpretation, and the more useful filter is "show me devices currently doing
 * something."
 */
export function isDeviceOn(device: AnyDevice): boolean {
  switch (device.type) {
    case DeviceType.Light:
    case DeviceType.Fan:
      return device.powerState === PowerState.On;
    case DeviceType.Thermostat:
      return device.state === ThermostatState.Heating
        || device.state === ThermostatState.Cooling;
    case DeviceType.DoorLock:
      return true; // Latch devices are always "on"
    default:
      const _exhaustive: never = device;
      return false;
  }
}

/**
 * "Off" specifically excludes latch devices:
 *   "latch devices are never 'off'"
 */
export function isDeviceOff(device: AnyDevice): boolean {
  if (device.type === DeviceType.DoorLock) return false;
  return !isDeviceOn(device);
}

/**
 * Apply all active filters to a device list. Filters are combined with AND —
 * a device must pass every filter to be included.
 */
export function applyFilters(
  devices: readonly AnyDevice[],
  filters: DeviceFilters,
): AnyDevice[] {
  return devices.filter((device) => {
    if (filters.powerStatus === 'on'  && !isDeviceOn(device))  return false;
    if (filters.powerStatus === 'off' && !isDeviceOff(device)) return false;

    if (filters.location !== null && device.location !== filters.location) {
      return false;
    }

    if (filters.types.size > 0 && !filters.types.has(device.type)) {
      return false;
    }

    return true;
  });
}

/**
 * Distinct locations present in the current device list, sorted alphabetically.
 * Used to populate the Location dropdown — we don't hard-code locations because
 * the user may register devices in arbitrary new locations at runtime.
 */
export function availableLocations(devices: readonly AnyDevice[]): string[] {
  const set = new Set<string>();
  for (const d of devices) set.add(d.location);
  return Array.from(set).sort((a, b) => a.localeCompare(b));
}

/**
 * True when no filter is active. Used by the UI to decide whether to show
 * the Clear button.
 */
export function isFilterActive(filters: DeviceFilters): boolean {
  return filters.powerStatus !== 'all'
    || filters.location !== null
    || filters.types.size > 0;
}
