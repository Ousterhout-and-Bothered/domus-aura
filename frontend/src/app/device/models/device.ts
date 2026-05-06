/**
 * Device type discriminator. String values match the backend's $type
 * polymorphism config (Program.cs JsonDerivedType registration) and
 * the JsonStringEnumConverter that serializes the C# `Type` enum as
 * a string on the wire.
 */
export enum DeviceType {
  Light = 'Light',
  Fan = 'Fan',
  Thermostat = 'Thermostat',
  DoorLock = 'DoorLock',
}

/**
 * Power state shared by Light, Fan, and (semantically) Thermostat.
 * Matches Domain/Device/PowerState.cs.
 */
export enum PowerState {
  Off = 'Off',
  On = 'On',
}

/**
 * Common shape for every device returned by the backend. The `$type`
 * field is the JSON polymorphism discriminator; the `type` field is
 * the underlying C# enum serialized as a string. Both are present
 * and equal — `$type` for type narrowing in TypeScript, `type` for
 * regular property access.
 */
export interface DeviceBase {
  $type: DeviceType;
  id: string;
  name: string;
  location: string;
  type: DeviceType;
}

/**
 * Command request body for PUT /api/devices/{id}/state.
 * Matches Contracts/Devices/DeviceCommandRequest.cs.
 *
 * Command names use camelCase: setPower, setBrightness, setColor, setSpeed,
 * setMode, setDesiredTemperature, lock, unlock.
 *
 * Value types by command:
 *   setPower             → 'On' | 'Off'
 *   setBrightness        → number (10-100)
 *   setColor             → string (hex, e.g. '#FF8800')
 *   setSpeed             → 'Low' | 'Medium' | 'High'
 *   setMode              → 'Heat' | 'Cool' | 'Auto'
 *   setDesiredTemperature → number (60-80)
 *   lock / unlock        → null
 */
export interface DeviceCommandRequest {
  command: string;
  value?: string | number | null;
}

/**
 * Filters for GET /api/devices. All optional.
 * `state` is 'on' | 'off' (string) — matches DeviceController.GetAll.
 */
export interface DeviceFilters {
  location?: string;
  type?: DeviceType;
  state?: 'on' | 'off';
}

/**
 * Body for POST /api/devices.
 * Matches Contracts/Devices/RegisterDeviceRequest.cs.
 */
export interface RegisterDeviceRequest {
  name: string;
  location: string;
  type: DeviceType;
}

/**
 * Body for PATCH /api/devices/{id}.
 * Matches Contracts/Devices/UpdateDeviceRequest.cs.
 *
 * Both fields are sent on every request; the server detects which
 * fields actually changed and logs only the deltas to command history.
 * If neither field changes, the call is a no-op (returns the unchanged
 * device, no audit entry written).
 */
export interface UpdateDeviceRequest {
  name: string;
  location: string;
}

/**
 * One row from GET /api/devices/{id}/history.
 * Matches Domain/Device/CommandHistory.cs.
 */
export interface CommandHistory {
  id: string;
  deviceId: string;
  operation: string;
  timestamp: string; // ISO 8601 UTC
}

/**
 * Filters for GET /api/devices/history. All optional.
 *
 * `from` and `to` are inclusive ISO 8601 UTC bounds.
 * `location` matches the device's current location.
 */
export interface HistoryFilters {
  page?: number;
  pageSize?: number;
  location?: string;
  deviceId?: string;
  from?: string;
  to?: string;
}

/**
 * Generic paged response envelope.
 * Matches Domain/Common/PagedResult.cs.
 *
 * `page` is 1-indexed. `total` is the unfiltered-by-page count
 * across all pages; client computes total pages as
 * Math.ceil(total / pageSize).
 */
export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

/**
 * SSE event emitted from /api/devices/events.
 * Matches Domain/Device/Events/DeviceChangedEvent.cs.
 */
export interface DeviceChangedEvent {
  deviceId: string;
  changeType: DeviceChangeType;
  payload: DeviceEventPayload;
}

export enum DeviceChangeType {
  Created = 'Created',
  Updated = 'Updated',
  Deleted = 'Deleted',
}

/**
 * SSE payload — a snapshot of the device after the change. Has the
 * same shape as the device list/detail responses. Typed loosely here
 * to avoid a circular import with device-types.ts; narrow at the
 * consumption site by checking $type.
 */
export type DeviceEventPayload = Record<string, unknown>;
