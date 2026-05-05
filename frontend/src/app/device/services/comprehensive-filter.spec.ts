import { AnyDevice } from '../models/device-types';
import { DeviceType, PowerState } from '../models/device';
import { ThermostatState, FanSpeed, ThermostatMode, DoorLockState } from '../models/device-types';
import { applyFilters, DEFAULT_FILTERS, isDeviceOn, isDeviceOff, DeviceFilters } from './filter';

describe('Comprehensive Filter Tests', () => {
  const devices: AnyDevice[] = [
    {
      id: '1',
      name: 'Living Room Light',
      location: 'Living Room',
      type: DeviceType.Light,
      powerState: PowerState.On,
      brightness: 80,
      colorHex: '#FFFFFF'
    },
    {
      id: '2',
      name: 'Kitchen Light',
      location: 'Kitchen',
      type: DeviceType.Light,
      powerState: PowerState.Off,
      brightness: 50,
      colorHex: '#FFFFFF'
    },
    {
      id: '3',
      name: 'Bedroom Fan',
      location: 'Bedroom',
      type: DeviceType.Fan,
      powerState: PowerState.On,
      speed: FanSpeed.Medium
    },
    {
      id: '4',
      name: 'Hallway Thermostat',
      location: 'Hallway',
      type: DeviceType.Thermostat,
      desiredTemperature: 72,
      ambientTemperature: 70,
      mode: ThermostatMode.Auto,
      state: ThermostatState.Heating
    },
    {
      id: '5',
      name: 'Living Room Thermostat',
      location: 'Living Room',
      type: DeviceType.Thermostat,
      desiredTemperature: 70,
      ambientTemperature: 70,
      mode: ThermostatMode.Auto,
      state: ThermostatState.Idle
    },
    {
      id: '6',
      name: 'Front Door Lock',
      location: 'Entry',
      type: DeviceType.DoorLock,
      lockState: DoorLockState.Locked
    }
  ];

  it('"All" filter shows all devices', () => {
    const result = applyFilters(devices, DEFAULT_FILTERS);
    expect(result.length).toBe(devices.length);
  });

  it('"On" filter shows only devices in an "on" state (includes all door locks)', () => {
    const filters: DeviceFilters = { ...DEFAULT_FILTERS, powerStatus: 'on' };
    const result = applyFilters(devices, filters);

    // 1 (Light On), 3 (Fan On), 4 (Thermostat Heating), 6 (Door Lock)
    expect(result.length).toBe(4);
    expect(result.map(d => d.id)).toContain('1');
    expect(result.map(d => d.id)).toContain('3');
    expect(result.map(d => d.id)).toContain('4');
    expect(result.map(d => d.id)).toContain('6');
  });

  it('"Off" filter shows only powered devices that are off (excludes door locks)', () => {
    const filters: DeviceFilters = { ...DEFAULT_FILTERS, powerStatus: 'off' };
    const result = applyFilters(devices, filters);

    // 2 (Light Off), 5 (Thermostat Idle - not heating/cooling)
    expect(result.length).toBe(2);
    expect(result.map(d => d.id)).toContain('2');
    expect(result.map(d => d.id)).toContain('5');
    expect(result.map(d => d.id)).not.toContain('6');
  });

  it('Location filter shows only devices in the selected location', () => {
    const filters: DeviceFilters = { ...DEFAULT_FILTERS, location: 'Living Room' };
    const result = applyFilters(devices, filters);

    expect(result.length).toBe(2);
    expect(result.every(d => d.location === 'Living Room')).toBe(true);
  });

  it('Device Type filter shows only devices of the selected type', () => {
    const filters: DeviceFilters = { ...DEFAULT_FILTERS, types: new Set([DeviceType.Light]) };
    const result = applyFilters(devices, filters);

    expect(result.length).toBe(2);
    expect(result.every(d => d.type === DeviceType.Light)).toBe(true);
  });

  it('Filters combine correctly ("On" + "Living Room")', () => {
    const filters: DeviceFilters = {
      powerStatus: 'on',
      location: 'Living Room',
      types: new Set()
    };
    const result = applyFilters(devices, filters);

    // Only 'Living Room Light' (On) matches.
    // 'Living Room Thermostat' is Idle, so it's "Off" by the isDeviceOn definition.
    expect(result.length).toBe(1);
    expect(result[0].id).toBe('1');
  });
});
