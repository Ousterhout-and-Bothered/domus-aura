import { DeviceBase, DeviceType, PowerState } from './device';

/* ─────────────── Light ─────────────── */

export interface Light extends DeviceBase {
  $type: DeviceType.Light;
  type: DeviceType.Light;
  powerState: PowerState;
  brightness: number; // 10-100
  colorHex: string;   // '#FFFFFF'
}

/* ─────────────── Fan ─────────────── */

export enum FanSpeed {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
}

export interface Fan extends DeviceBase {
  $type: DeviceType.Fan;
  type: DeviceType.Fan;
  powerState: PowerState;
  speed: FanSpeed;
}

/* ─────────────── Thermostat ─────────────── */

export enum ThermostatMode {
  Heat = 'Heat',
  Cool = 'Cool',
  Auto = 'Auto',
}

export enum ThermostatState {
  Off = 'Off',
  Idle = 'Idle',
  Heating = 'Heating',
  Cooling = 'Cooling',
}

export interface Thermostat extends DeviceBase {
  $type: DeviceType.Thermostat;
  type: DeviceType.Thermostat;
  powerState: PowerState;        // inherited from PoweredDevice on the backend
  state: ThermostatState;
  mode: ThermostatMode;
  desiredTemperature: number;    // 60-80 °F
  ambientTemperature: number;    // -40 to 150 °F per validator
}

/* ─────────────── DoorLock ─────────────── */

export enum DoorLockState {
  Locked = 'Locked',
  Unlocked = 'Unlocked',
}

export interface DoorLock extends DeviceBase {
  $type: DeviceType.DoorLock;
  type: DeviceType.DoorLock;
  lockState: DoorLockState;
}

/* ─────────────── Discriminated union ─────────────── */

/**
 * Use this type wherever you accept "any device". Switch on `device.type`
 * (or `device.$type`) to narrow to the specific shape:
 *
 *   switch (device.type) {
 *     case DeviceType.Light:      // device is Light here
 *     case DeviceType.Thermostat: // device is Thermostat here
 *   }
 */
export type AnyDevice = Light | Fan | Thermostat | DoorLock;

/* ─────────────── Type guards (for use outside switches) ─────────────── */

export const isLight = (d: AnyDevice): d is Light => d.type === DeviceType.Light;
export const isFan = (d: AnyDevice): d is Fan => d.type === DeviceType.Fan;
export const isThermostat = (d: AnyDevice): d is Thermostat => d.type === DeviceType.Thermostat;
export const isDoorLock = (d: AnyDevice): d is DoorLock => d.type === DeviceType.DoorLock;
