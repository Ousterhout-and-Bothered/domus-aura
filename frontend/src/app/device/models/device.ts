/**
 * Device type discriminator. String values match the backend's $type
 * polymorphism config (Program.cs JsonDerivedType registration).
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
 * Common shape for every device type. The `$type` field is the discriminator
 * the backend writes — TypeScript narrows the union via this field.
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
 * One row from GET /api/devices/{id}/history.
 * Matches Domain/Device/CommandHistory.cs.
 */
export interface CommandHistory {
  id: string;
  deviceId: string;
  action: string;
  timestamp: string; // ISO 8601
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
 * SSE payload — a snapshot of the device after the change.
 * Same shape as the device list/detail responses, so reuse the AnyDevice union.
 * Defined as `unknown` here and narrowed at the consumption site to avoid a
 * circular import between this file and device-types.ts.
 */
export type DeviceEventPayload = Record<string, unknown>;
