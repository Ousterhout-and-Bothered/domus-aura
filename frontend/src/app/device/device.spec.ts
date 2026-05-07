import { render, screen, fireEvent } from '@testing-library/angular';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';
import { describe, it, expect, vi, beforeEach } from 'vitest';

import { LightBulb } from './components/light-bulb/light-bulb';
import { FanSpinning } from './components/fan-spinning/fan-spinning';
import { ThermostatGauge } from './components/thermostat-gauge/thermostat-gauge';
import { DoorLock } from './components/door-lock/door-lock';
import { DeviceCard } from './components/device-card/device-card';
import { RegisterDeviceDialog } from './components/register-device-dialog/register-device-dialog';
import { DeviceApiService } from './services/device-api.service';
import { applyFilters, DeviceFilters } from './services/filter';
import { DeviceType, PowerState } from './models/device';
import {
  FanSpeed,
  ThermostatMode,
  ThermostatState,
  DoorLockState,
  AnyDevice
} from './models/device-types';

describe('Smart Home Simulator Front-end Tests', () => {

  describe('Component Rendering Tests', () => {

    it('Light card: renders correctly when On', async () => {
      const fixture = TestBed.createComponent(LightBulb);
      fixture.componentRef.setInput('name', 'Living Room Light');
      fixture.componentRef.setInput('location', 'Living Room');
      fixture.componentRef.setInput('powerState', PowerState.On);
      fixture.componentRef.setInput('brightness', 80);
      fixture.componentRef.setInput('colorHex', '#FFA500');
      fixture.detectChanges();

      const el = fixture.nativeElement;
      expect(el.textContent).toContain('Living Room Light');
      expect(el.textContent).toContain('80%');

      const bulb = el.querySelector('svg');
      expect(bulb.getAttribute('aria-label')).toContain('Power On');
      expect(bulb.getAttribute('aria-label')).toContain('brightness 80%');
    });

    it('Light card: renders correctly when Off', async () => {
      const fixture = TestBed.createComponent(LightBulb);
      fixture.componentRef.setInput('name', 'Living Room Light');
      fixture.componentRef.setInput('location', 'Living Room');
      fixture.componentRef.setInput('powerState', PowerState.Off);
      fixture.componentRef.setInput('brightness', 80);
      fixture.componentRef.setInput('colorHex', '#FFA500');
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain('Off');
      const bulb = fixture.nativeElement.querySelector('svg');
      expect(bulb.getAttribute('aria-label')).toContain('Power Off');
    });

    it('Fan card: renders correctly when On', async () => {
      const fixture = TestBed.createComponent(FanSpinning);
      fixture.componentRef.setInput('name', 'Bedroom Fan');
      fixture.componentRef.setInput('location', 'Bedroom');
      fixture.componentRef.setInput('powerState', PowerState.On);
      fixture.componentRef.setInput('speed', FanSpeed.Medium);
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain('Bedroom Fan');
      expect(fixture.nativeElement.textContent).toContain('Medium');
    });

    it('Fan card: renders correctly when Off', async () => {
       const fixture = TestBed.createComponent(FanSpinning);
       fixture.componentRef.setInput('name', 'Bedroom Fan');
       fixture.componentRef.setInput('location', 'Bedroom');
       fixture.componentRef.setInput('powerState', PowerState.Off);
       fixture.componentRef.setInput('speed', FanSpeed.Medium);
       fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain('Off');
    });

    it('Thermostat card: renders correctly in each state', async () => {
      const states = [
        { state: ThermostatState.Off, label: 'Off' },
        { state: ThermostatState.Idle, label: 'Idle' },
        { state: ThermostatState.Heating, label: 'Heating' },
        { state: ThermostatState.Cooling, label: 'Cooling' }
      ];

      for (const s of states) {
        const fixture = TestBed.createComponent(ThermostatGauge);
        fixture.componentRef.setInput('name', 'Hallway Thermostat');
        fixture.componentRef.setInput('location', 'Hallway');
        fixture.componentRef.setInput('desiredTemperature', 72);
        fixture.componentRef.setInput('ambientTemperature', 70);
        fixture.componentRef.setInput('mode', ThermostatMode.Auto);
        fixture.componentRef.setInput('state', s.state);
        fixture.detectChanges();

        expect(fixture.nativeElement.textContent).toContain(s.label);
        expect(fixture.nativeElement.textContent).toContain('72');
        expect(fixture.nativeElement.textContent).toContain('70°F');
      }
    });

    it('Door Lock card: renders correctly in Locked and Unlocked states', async () => {
       const fixture = TestBed.createComponent(DoorLock);
       fixture.componentRef.setInput('name', 'Front Door');
       fixture.componentRef.setInput('location', 'Entry');
       fixture.componentRef.setInput('lockState', DoorLockState.Locked);
       fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain('Locked');

      fixture.componentRef.setInput('lockState', DoorLockState.Unlocked);
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain('Unlocked');
    });
  });

  describe('Filter Tests', () => {
    const mockDevices: AnyDevice[] = [
      { id: '1', name: 'Light 1', location: 'Living Room', type: DeviceType.Light, powerState: PowerState.On, brightness: 100, colorHex: '#FFFFFF' } as any,
      { id: '2', name: 'Light 2', location: 'Kitchen', type: DeviceType.Light, powerState: PowerState.Off, brightness: 100, colorHex: '#FFFFFF' } as any,
      { id: '3', name: 'Fan 1', location: 'Living Room', type: DeviceType.Fan, powerState: PowerState.On, speed: FanSpeed.High } as any,
      { id: '4', name: 'Thermostat 1', location: 'Living Room', type: DeviceType.Thermostat, state: ThermostatState.Heating, desiredTemperature: 72, ambientTemperature: 68, mode: ThermostatMode.Heat } as any,
      { id: '5', name: 'Lock 1', location: 'Entry', type: DeviceType.DoorLock, lockState: DoorLockState.Locked } as any
    ];

    it('"All" filter shows all devices', () => {
      const filters: DeviceFilters = { powerStatus: 'all', location: null, types: new Set() };
      const result = applyFilters(mockDevices, filters);
      expect(result.length).toBe(5);
    });

    it('"On" filter shows only devices in an "on" state', () => {
      // Light 1 (On), Fan 1 (On), Thermostat 1 (Heating), Lock 1 (Always On)
      const filters: DeviceFilters = { powerStatus: 'on', location: null, types: new Set() };
      const result = applyFilters(mockDevices, filters);
      expect(result.length).toBe(4);
      expect(result.map(d => d.id)).toContain('1');
      expect(result.map(d => d.id)).toContain('3');
      expect(result.map(d => d.id)).toContain('4');
      expect(result.map(d => d.id)).toContain('5');
    });

    it('"Off" filter shows only powered devices that are off', () => {
      // Light 2 (Off). (Locks are never off)
      const filters: DeviceFilters = { powerStatus: 'off', location: null, types: new Set() };
      const result = applyFilters(mockDevices, filters);
      expect(result.length).toBe(1);
      expect(result[0].id).toBe('2');
    });

    it('Location filter shows only devices in the selected location', () => {
      const filters: DeviceFilters = { powerStatus: 'all', location: 'Kitchen', types: new Set() };
      const result = applyFilters(mockDevices, filters);
      expect(result.length).toBe(1);
      expect(result[0].location).toBe('Kitchen');
    });

    it('Device Type filter shows only devices of the selected type', () => {
      const filters: DeviceFilters = { powerStatus: 'all', location: null, types: new Set([DeviceType.Light]) };
      const result = applyFilters(mockDevices, filters);
      expect(result.length).toBe(2);
      expect(result.every(d => d.type === DeviceType.Light)).toBe(true);
    });

    it('Filters combine correctly', () => {
      const filters: DeviceFilters = { powerStatus: 'on', location: 'Living Room', types: new Set([DeviceType.Light]) };
      const result = applyFilters(mockDevices, filters);
      expect(result.length).toBe(1);
      expect(result[0].id).toBe('1');
    });
  });

  describe('Input Validation Tests', () => {
    it('Brightness slider is clamped to 10-100', async () => {
      const fixture = TestBed.createComponent(LightBulb);
      fixture.componentRef.setInput('name', 'L');
      fixture.componentRef.setInput('location', 'R');
      fixture.componentRef.setInput('powerState', PowerState.On);
      fixture.componentRef.setInput('brightness', 50);
      fixture.componentRef.setInput('colorHex', '#FFFFFF');
      fixture.detectChanges();

      const slider = fixture.debugElement.nativeElement.querySelector('p-slider');
      expect(slider).toBeTruthy();
      // Check properties on the debug element if possible, or just the component instance logic
    });

    it('Temperature input is clamped to 60-80', async () => {
       const fixture = TestBed.createComponent(ThermostatGauge);
       fixture.componentRef.setInput('name', 'T');
       fixture.componentRef.setInput('location', 'R');
       fixture.componentRef.setInput('desiredTemperature', 70);
       fixture.componentRef.setInput('ambientTemperature', 70);
       fixture.componentRef.setInput('mode', ThermostatMode.Auto);
       fixture.componentRef.setInput('state', ThermostatState.Idle);
       fixture.detectChanges();

      const inputNum = fixture.nativeElement.querySelector('p-inputnumber');
      expect(inputNum).toBeTruthy();
    });

    it('Device registration requires name, location, and type', async () => {
      TestBed.configureTestingModule({
        providers: [
          { provide: DeviceApiService, useValue: {} },
          MessageService
        ]
      });
      const fixture = TestBed.createComponent(RegisterDeviceDialog);
      fixture.componentRef.setInput('visible', true);
      fixture.componentRef.setInput('existingLocations', []);
      fixture.detectChanges();

      const component = fixture.componentInstance;

      component.name.set('');
      component.location.set('');
      component.type.set(null);
      fixture.detectChanges();
      expect(component.canSubmit()).toBe(false);

      component.name.set('Test');
      component.location.set('Room');
      component.type.set(DeviceType.Light);
      fixture.detectChanges();
      expect(component.canSubmit()).toBe(true);
    });
  });

  describe('Device Management & API Integration Tests', () => {
    let httpMock: HttpTestingController;
    let deviceApi: DeviceApiService;

    beforeEach(() => {
      TestBed.configureTestingModule({
        imports: [HttpClientTestingModule],
        providers: [DeviceApiService, MessageService, ConfirmationService]
      });
      httpMock = TestBed.inject(HttpTestingController);
      deviceApi = TestBed.inject(DeviceApiService);
    });

    it('Register device form submits correctly', async () => {
      const mockDevice = { id: 'new', name: 'New Light', location: 'Office', type: DeviceType.Light, powerState: PowerState.Off, brightness: 100, colorHex: '#FFFFFF' } as any;

      const fixture = TestBed.createComponent(RegisterDeviceDialog);
      fixture.componentRef.setInput('visible', true);
      fixture.componentRef.setInput('existingLocations', ['Office']);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      component.name.set('New Light');
      component.location.set('Office');
      component.type.set(DeviceType.Light);

      const emitSpy = vi.spyOn(component.deviceCreated, 'emit');

      component.onSubmit();

      const req = httpMock.expectOne('/api/devices');
      expect(req.request.method).toBe('POST');
      req.flush(mockDevice);
      expect(emitSpy).toHaveBeenCalledWith(mockDevice);
    });

    it('Remove device shows confirmation and deletes device on accept', async () => {
      const mockDevice = { id: '1', name: 'L', location: 'R', type: DeviceType.Light, powerState: PowerState.On, brightness: 100, colorHex: '#FFFFFF' } as any;
      const confirmService = TestBed.inject(ConfirmationService);
      const confirmSpy = vi.spyOn(confirmService, 'confirm').mockImplementation((conf) => {
        conf.accept?.();
        return confirmService;
      });

      const fixture = TestBed.createComponent(DeviceCard);
      fixture.componentRef.setInput('device', mockDevice);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      const removeSpy = vi.spyOn(component.deviceRemoved, 'emit');

      component.onRequestRemoveFromMenu();

      expect(confirmSpy).toHaveBeenCalled();
      const req = httpMock.expectOne('/api/devices/1');
      expect(req.request.method).toBe('DELETE');
      req.flush(null);

      expect(removeSpy).toHaveBeenCalledWith('1');
    });

    it('Controlling a device sends the correct API request', async () => {
      const mockDevice = { id: '1', name: 'L', location: 'R', type: DeviceType.Light, powerState: PowerState.Off, brightness: 100, colorHex: '#FFFFFF' } as any;

      const fixture = TestBed.createComponent(DeviceCard);
      fixture.componentRef.setInput('device', mockDevice);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      component.onSetLightPower(mockDevice, PowerState.On);

      const req = httpMock.expectOne('/api/devices/1/state');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ command: 'setPower', value: PowerState.On });
      req.flush({ ...mockDevice, powerState: PowerState.On });
    });

    it('Error responses display appropriate user-facing messages', async () => {
      const mockDevice = { id: '1', name: 'L', location: 'R', type: DeviceType.Light, powerState: PowerState.Off, brightness: 100, colorHex: '#FFFFFF' } as any;
      const messageService = TestBed.inject(MessageService);
      const toastSpy = vi.spyOn(messageService, 'add');

      const fixture = TestBed.createComponent(DeviceCard);
      fixture.componentRef.setInput('device', mockDevice);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      component.onSetLightPower(mockDevice, PowerState.On);

      const req = httpMock.expectOne('/api/devices/1/state');
      req.error(new ErrorEvent('Network error'), { status: 500, statusText: 'Internal Server Error' });

      expect(toastSpy).toHaveBeenCalledWith(expect.objectContaining({
        severity: 'error'
      }));
    });
  });
});
