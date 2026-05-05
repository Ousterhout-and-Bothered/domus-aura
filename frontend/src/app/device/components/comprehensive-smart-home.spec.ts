import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { LightBulb } from './light-bulb/light-bulb';
import { FanSpinning } from './fan-spinning/fan-spinning';
import { ThermostatGauge } from './thermostat-gauge/thermostat-gauge';
import { DoorLock } from './door-lock/door-lock';
import { DeviceList } from './device-list/device-list';
import { RegisterDeviceDialog } from './register-device-dialog/register-device-dialog';
import { DeviceCard } from './device-card/device-card';

import { PowerState, DeviceType } from '../models/device';
import { FanSpeed, ThermostatMode, ThermostatState, DoorLockState, AnyDevice } from '../models/device-types';
import { DeviceApiService } from '../services/device-api.service';
import { DeviceEventService } from '../services/device-event.service';
import { ConfirmationService, MessageService } from 'primeng/api';

describe('Comprehensive Smart Home Simulator Tests', () => {

  describe('Component Rendering Tests', () => {

    describe('Light card', () => {
      let fixture: ComponentFixture<LightBulb>;
      let component: LightBulb;

      beforeEach(async () => {
        await TestBed.configureTestingModule({
          imports: [LightBulb],
          providers: [provideNoopAnimations()]
        }).compileComponents();
        fixture = TestBed.createComponent(LightBulb);
        component = fixture.componentInstance;
      });

      it('renders correctly when On (shows brightness slider, color picker)', async () => {
        fixture.componentRef.setInput('name', 'Test Light');
        fixture.componentRef.setInput('location', 'Living Room');
        fixture.componentRef.setInput('powerState', PowerState.On);
        fixture.componentRef.setInput('brightness', 80);
        fixture.componentRef.setInput('colorHex', '#FF0000');
        fixture.detectChanges();

        const slider = fixture.debugElement.query(By.css('p-slider'));
        const colorPicker = fixture.debugElement.query(By.css('p-colorpicker'));

        expect(slider).toBeTruthy();
        expect(colorPicker).toBeTruthy();
        expect(slider.componentInstance.disabled).toBeFalsy();
        expect(colorPicker.componentInstance.disabled).toBeFalsy();
      });

      it('renders correctly when Off (controls disabled)', async () => {
        fixture.componentRef.setInput('name', 'Test Light');
        fixture.componentRef.setInput('location', 'Living Room');
        fixture.componentRef.setInput('powerState', PowerState.Off);
        fixture.componentRef.setInput('brightness', 80);
        fixture.componentRef.setInput('colorHex', '#FF0000');
        fixture.detectChanges();

        const slider = fixture.debugElement.query(By.css('p-slider'));
        const colorPicker = fixture.debugElement.query(By.css('p-colorpicker'));

        expect(slider.componentInstance.disabled).toBeTruthy();
        expect(colorPicker.componentInstance.disabled).toBeTruthy();
      });
    });

    describe('Fan card', () => {
      let fixture: ComponentFixture<FanSpinning>;
      let component: FanSpinning;

      beforeEach(async () => {
        await TestBed.configureTestingModule({
          imports: [FanSpinning],
          providers: [provideNoopAnimations()]
        }).compileComponents();
        fixture = TestBed.createComponent(FanSpinning);
        component = fixture.componentInstance;
      });

      it('renders correctly when On (shows speed buttons with active state)', async () => {
        fixture.componentRef.setInput('name', 'Test Fan');
        fixture.componentRef.setInput('location', 'Bedroom');
        fixture.componentRef.setInput('powerState', PowerState.On);
        fixture.componentRef.setInput('speed', FanSpeed.Medium);
        fixture.detectChanges();

        const selectButton = fixture.debugElement.query(By.css('p-selectbutton'));
        expect(selectButton).toBeTruthy();
        expect(selectButton.componentInstance.disabled).toBeFalsy();
        expect(fixture.nativeElement.textContent).toContain('Medium');
      });

      it('renders correctly when Off (speed buttons disabled)', async () => {
        fixture.componentRef.setInput('name', 'Test Fan');
        fixture.componentRef.setInput('location', 'Bedroom');
        fixture.componentRef.setInput('powerState', PowerState.Off);
        fixture.componentRef.setInput('speed', FanSpeed.Medium);
        fixture.detectChanges();

        const selectButton = fixture.debugElement.query(By.css('p-selectbutton'));
        expect(selectButton.componentInstance.disabled).toBeTruthy();
      });
    });

    describe('Thermostat card', () => {
      let fixture: ComponentFixture<ThermostatGauge>;
      let component: ThermostatGauge;

      beforeEach(async () => {
        await TestBed.configureTestingModule({
          imports: [ThermostatGauge],
          providers: [provideNoopAnimations()]
        }).compileComponents();
        fixture = TestBed.createComponent(ThermostatGauge);
        component = fixture.componentInstance;
      });

      const states = [
        { state: ThermostatState.Off, label: 'Off' },
        { state: ThermostatState.Idle, label: 'Idle' },
        { state: ThermostatState.Heating, label: 'Heating' },
        { state: ThermostatState.Cooling, label: 'Cooling' }
      ];

      states.forEach(({ state, label }) => {
        it(`renders correctly in ${label} state`, async () => {
          fixture.componentRef.setInput('name', 'Test Thermo');
          fixture.componentRef.setInput('location', 'Hall');
          fixture.componentRef.setInput('desiredTemperature', 72);
          fixture.componentRef.setInput('ambientTemperature', 70);
          fixture.componentRef.setInput('mode', ThermostatMode.Auto);
          fixture.componentRef.setInput('state', state);
          fixture.detectChanges();

          expect(fixture.nativeElement.textContent).toContain(label);
          expect(fixture.nativeElement.textContent).toContain('72');
          expect(fixture.nativeElement.textContent).toContain('70');
        });
      });
    });

    describe('Door Lock card', () => {
      let fixture: ComponentFixture<DoorLock>;
      let component: DoorLock;

      beforeEach(async () => {
        await TestBed.configureTestingModule({
          imports: [DoorLock],
          providers: [provideNoopAnimations()]
        }).compileComponents();
        fixture = TestBed.createComponent(DoorLock);
        component = fixture.componentInstance;
      });

      it('renders correctly in Locked state', async () => {
        fixture.componentRef.setInput('name', 'Front Door');
        fixture.componentRef.setInput('location', 'Entry');
        fixture.componentRef.setInput('lockState', DoorLockState.Locked);
        fixture.detectChanges();

        expect(fixture.nativeElement.textContent).toContain('Locked');
        const shackle = fixture.debugElement.query(By.css('.shackle'));
        expect(shackle.classes['shackle-open']).toBeFalsy();
      });

      it('renders correctly in Unlocked state', async () => {
        fixture.componentRef.setInput('name', 'Front Door');
        fixture.componentRef.setInput('location', 'Entry');
        fixture.componentRef.setInput('lockState', DoorLockState.Unlocked);
        fixture.detectChanges();

        expect(fixture.nativeElement.textContent).toContain('Unlocked');
        const shackle = fixture.debugElement.query(By.css('.shackle'));
        expect(shackle.classes['shackle-open']).toBeTruthy();
      });
    });
  });

  describe('Filter Tests', () => {
    // These logic tests are best performed on the filter functions directly
    // but the issue asks for comprehensive tests of the front-end code.
    // I will mock the DeviceList and verify its computed 'rooms' or 'filteredDevices'.

    let fixture: ComponentFixture<DeviceList>;
    let component: DeviceList;
    let deviceApi: any;

    const mockDevices: AnyDevice[] = [
      { id: '1', name: 'Light 1', location: 'Living Room', type: DeviceType.Light, powerState: PowerState.On, brightness: 50, colorHex: '#FFFFFF' } as Light,
      { id: '2', name: 'Light 2', location: 'Living Room', type: DeviceType.Light, powerState: PowerState.Off, brightness: 50, colorHex: '#FFFFFF' } as Light,
      { id: '3', name: 'Lock 1', location: 'Entry', type: DeviceType.DoorLock, lockState: DoorLockState.Locked } as DoorLockDevice,
      { id: '4', name: 'Fan 1', location: 'Bedroom', type: DeviceType.Fan, powerState: PowerState.Off, speed: FanSpeed.Low } as Fan,
    ];

    beforeEach(async () => {
      deviceApi = {
        getAll: () => of(mockDevices),
        getById: (id: string) => of(mockDevices.find(d => d.id === id))
      };

      await TestBed.configureTestingModule({
        imports: [DeviceList],
        providers: [
          { provide: DeviceApiService, useValue: deviceApi },
          { provide: DeviceEventService, useValue: { events$: of(), connected: () => true, connect: () => {}, disconnect: () => {} } },
          MessageService,
          ConfirmationService,
          provideNoopAnimations()
        ]
      }).compileComponents();

      fixture = TestBed.createComponent(DeviceList);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('"All" filter shows all devices', () => {
      component.onFiltersChange({ status: 'all', location: 'all', type: 'all' });
      expect(component.filteredDevices().length).toBe(4);
    });

    it('"On" filter shows only devices in an "on" state (includes all door locks)', () => {
      component.onFiltersChange({ status: 'on', location: 'all', type: 'all' });
      // Light 1 (on) + Lock 1 (always "on" for filter)
      expect(component.filteredDevices().length).toBe(2);
      expect(component.filteredDevices().every(d => d.id === '1' || d.id === '3')).toBeTruthy();
    });

    it('"Off" filter shows only powered devices that are off (excludes door locks)', () => {
      component.onFiltersChange({ status: 'off', location: 'all', type: 'all' });
      // Light 2 (off) + Fan 1 (off). Lock 1 is excluded.
      expect(component.filteredDevices().length).toBe(2);
      expect(component.filteredDevices().every(d => d.id === '2' || d.id === '4')).toBeTruthy();
    });

    it('Location filter shows only devices in the selected location', () => {
      component.onFiltersChange({ status: 'all', location: 'Living Room', type: 'all' });
      expect(component.filteredDevices().length).toBe(2);
      expect(component.filteredDevices().every(d => d.location === 'Living Room')).toBeTruthy();
    });

    it('Device Type filter shows only devices of the selected type', () => {
      component.onFiltersChange({ status: 'all', location: 'all', type: DeviceType.DoorLock });
      expect(component.filteredDevices().length).toBe(1);
      expect(component.filteredDevices()[0].type).toBe(DeviceType.DoorLock);
    });

    it('Filters combine correctly (e.g., "On" + "Living Room")', () => {
      component.onFiltersChange({ status: 'on', location: 'Living Room', type: 'all' });
      expect(component.filteredDevices().length).toBe(1);
      expect(component.filteredDevices()[0].id).toBe('1');
    });
  });

  describe('Input Validation Tests', () => {
    it('Brightness slider is clamped to 10-100', async () => {
      await TestBed.configureTestingModule({
        imports: [LightBulb],
        providers: [provideNoopAnimations()]
      }).compileComponents();
      const fixture = TestBed.createComponent(LightBulb);
      fixture.componentRef.setInput('name', 'T');
      fixture.componentRef.setInput('location', 'L');
      fixture.componentRef.setInput('powerState', PowerState.On);
      fixture.componentRef.setInput('brightness', 50);
      fixture.componentRef.setInput('colorHex', '#FFFFFF');
      fixture.detectChanges();

      const slider = fixture.debugElement.query(By.css('p-slider')).componentInstance;
      expect(slider.min).toBe(10);
      expect(slider.max).toBe(100);
    });

    it('Temperature input is clamped to 60-80', async () => {
      await TestBed.configureTestingModule({
        imports: [ThermostatGauge],
        providers: [provideNoopAnimations()]
      }).compileComponents();
      const fixture = TestBed.createComponent(ThermostatGauge);
      fixture.componentRef.setInput('name', 'T');
      fixture.componentRef.setInput('location', 'L');
      fixture.componentRef.setInput('desiredTemperature', 70);
      fixture.componentRef.setInput('ambientTemperature', 70);
      fixture.componentRef.setInput('mode', ThermostatMode.Auto);
      fixture.componentRef.setInput('state', ThermostatState.Idle);
      fixture.detectChanges();

      const input = fixture.debugElement.query(By.css('p-inputnumber')).componentInstance;
      expect(input.min).toBe(60);
      expect(input.max).toBe(80);
    });

    it('Device registration requires name, location, and type', async () => {
      await TestBed.configureTestingModule({
        imports: [RegisterDeviceDialog],
        providers: [
          { provide: DeviceApiService, useValue: {} },
          MessageService,
          provideNoopAnimations()
        ]
      }).compileComponents();
      const fixture = TestBed.createComponent(RegisterDeviceDialog);
      fixture.componentRef.setInput('visible', true);
      fixture.componentRef.setInput('existingLocations', []);
      fixture.detectChanges();

      const component = fixture.componentInstance;

      expect(component.canSubmit()).toBeFalsy();

      component.type.set(DeviceType.Light);
      fixture.detectChanges();
      expect(component.canSubmit()).toBeFalsy();

      component.name.set('Lamp');
      fixture.detectChanges();
      expect(component.canSubmit()).toBeFalsy();

      component.location.set('Living Room');
      fixture.detectChanges();
      expect(component.canSubmit()).toBeTruthy();
    });
  });

  describe('Device Management Tests', () => {
    it('Register device form submits correctly and new device appears', async () => {
      const mockApi = {
        register: (req: any) => of({ id: 'new', ...req, powerState: PowerState.Off, brightness: 100, colorHex: '#FFFFFF' })
      };
      await TestBed.configureTestingModule({
        imports: [RegisterDeviceDialog],
        providers: [
          { provide: DeviceApiService, useValue: mockApi },
          MessageService,
          provideNoopAnimations()
        ]
      }).compileComponents();
      const fixture = TestBed.createComponent(RegisterDeviceDialog);
      fixture.componentRef.setInput('visible', true);
      fixture.componentRef.setInput('existingLocations', []);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      component.type.set(DeviceType.Light);
      component.name.set('New Light');
      component.location.set('New Room');

      let createdDevice: any;
      component.deviceCreated.subscribe(d => createdDevice = d);

      component.onSubmit();
      expect(createdDevice).toBeTruthy();
      expect(createdDevice.name).toBe('New Light');
    });

    it('Remove device shows confirmation dialog', async () => {
      const confirmService = jasmine.createSpyObj('ConfirmationService', ['confirm']);
      const mockDevice: Light = { id: '1', name: 'L', location: 'R', type: DeviceType.Light, powerState: PowerState.On, brightness: 50, colorHex: '#F' };

      await TestBed.configureTestingModule({
        imports: [DeviceCard],
        providers: [
          { provide: DeviceApiService, useValue: {} },
          { provide: ConfirmationService, useValue: confirmService },
          MessageService,
          provideNoopAnimations()
        ]
      }).compileComponents();

      const fixture = TestBed.createComponent(DeviceCard);
      fixture.componentRef.setInput('device', mockDevice);
      fixture.detectChanges();

      const removeBtn = fixture.debugElement.query(By.css('.device-card-remove'));
      removeBtn.nativeElement.click();

      expect(confirmService.confirm).toHaveBeenCalled();
      const args = confirmService.confirm.calls.mostRecent().args[0];
      expect(args.header).toBe('Remove device');
    });
  });

  describe('API Integration Tests (with mocked HTTP)', () => {
    let httpMock: HttpTestingController;
    let fixture: ComponentFixture<DeviceList>;
    let component: DeviceList;

    beforeEach(async () => {
      await TestBed.configureTestingModule({
        imports: [DeviceList],
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          DeviceApiService,
          DeviceEventService,
          MessageService,
          ConfirmationService,
          provideNoopAnimations()
        ]
      }).compileComponents();

      httpMock = TestBed.inject(HttpTestingController);
      fixture = TestBed.createComponent(DeviceList);
      component = fixture.componentInstance;
    });

    afterEach(() => {
      httpMock.verify();
    });

    it('Device list loads on component mount', () => {
      fixture.detectChanges(); // ngOnInit

      const req = httpMock.expectOne('/api/devices');
      expect(req.request.method).toBe('GET');
      req.flush([{ id: '1', name: 'L', location: 'R', type: DeviceType.Light }]);

      expect(component.devices().length).toBe(1);
    });

    it('Controlling a device sends the correct API request', async () => {
      // Need DeviceCard for this
      const mockLight: Light = { id: 'L1', name: 'Light', location: 'Room', type: DeviceType.Light, powerState: PowerState.On, brightness: 50, colorHex: '#FFFFFF' };

      const cardFixture = TestBed.createComponent(DeviceCard);
      cardFixture.componentRef.setInput('device', mockLight);
      cardFixture.detectChanges();

      // Find the LightBulb component inside
      const lightBulb = cardFixture.debugElement.query(By.directive(LightBulb)).componentInstance;
      lightBulb.powerStateChange.emit(PowerState.Off);

      const req = httpMock.expectOne('/api/devices/L1/command');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ command: 'setPower', value: PowerState.Off });
      req.flush({ ...mockLight, powerState: PowerState.Off });
    });

    it('Error responses display appropriate user-facing messages', () => {
      fixture.detectChanges();
      const req = httpMock.expectOne('/api/devices');
      req.error(new ProgressEvent('error'), { status: 500, statusText: 'Internal Server Error' });

      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain('Could not load devices');
    });
  });
});
