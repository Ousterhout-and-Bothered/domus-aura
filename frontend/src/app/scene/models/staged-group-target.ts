import { DeviceType } from '../../device/models/device';

/**
 * A staged group target — a rule that resolves to multiple devices at
 * execute time, rather than a fixed list of specific devices.
 */
export interface StagedGroupTarget {
  /** The device type this group targets. Required. */
  deviceType: DeviceType;

  /**
   * Optional location scope. Null matches any location.
   */
  location: string | null;

  /**
   * Stable composite identifier for view-tracking and deduplication.
   * Built from (deviceType, location); two groups with the same
   * deviceType + location share the same id. Use makeGroupTargetId()
   * to construct rather than building strings manually.
   */
  id: string;
}

/**
 * Builds the composite identifier used for view-tracking and dedup.
 */
export function makeGroupTargetId(
  deviceType: DeviceType,
  location: string | null,
): string {
  return location === null
    ? `group:${deviceType}`
    : `group:${deviceType}:${location}`;
}

/**
 * Constructs a StagedGroupTarget given a deviceType and optional
 * location. Centralizes id generation so all callers stay consistent.
 */
export function makeStagedGroupTarget(
  deviceType: DeviceType,
  location: string | null,
): StagedGroupTarget {
  return {
    deviceType,
    location,
    id: makeGroupTargetId(deviceType, location),
  };
}
