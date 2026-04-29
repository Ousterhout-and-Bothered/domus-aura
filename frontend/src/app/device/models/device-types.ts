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
  // Note: Thermostat does NOT have a separate powerState. The Off/Idle/
  // Heating/Cooling state machine encodes power directly — Off is a
  // first-class state, transitioning to Idle on power-on. This matches
  // the domain model (Thermostat does not implement IPowerable).
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

export type AnyDevice = Light | Fan | Thermostat | DoorLock;

/* ─────────────── Type guards (for use outside switches) ─────────────── */

export const isLight = (d: AnyDevice): d is Light => d.type === DeviceType.Light;
export const isFan = (d: AnyDevice): d is Fan => d.type === DeviceType.Fan;
export const isThermostat = (d: AnyDevice): d is Thermostat => d.type === DeviceType.Thermostat;
export const isDoorLock = (d: AnyDevice): d is DoorLock => d.type === DeviceType.DoorLock;
