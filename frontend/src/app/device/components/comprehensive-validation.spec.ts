import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LightBulb } from './light-bulb/light-bulb';
import { ThermostatGauge } from './thermostat-gauge/thermostat-gauge';
import { RegisterDeviceDialog } from './register-device-dialog/register-device-dialog';
import { PowerState, DeviceType } from '../models/device';
import { ThermostatMode, ThermostatState } from '../models/device-types';
import { By } from '@angular/platform-browser';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { DeviceApiService } from '../services/device-api.service';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';

describe('Comprehensive Validation Tests', () => {

  describe('Brightness Slider Clamping', () => {
    let fixture: ComponentFixture<LightBulb>;

    beforeEach(async () => {
      await TestBed.configureTestingModule({
        imports: [LightBulb],
        providers: [provideNoopAnimations()]
      }).compileComponents();
      fixture = TestBed.createComponent(LightBulb);
    });

    it('should have min 10 and max 100 on the slider', async () => {
      fixture.componentRef.setInput('name', 'Light');
      fixture.componentRef.setInput('location', 'Room');
      fixture.componentRef.setInput('powerState', PowerState.On);
      fixture.componentRef.setInput('brightness', 50);
      fixture.componentRef.setInput('colorHex', '#FFFFFF');
      fixture.detectChanges();
      await fixture.whenStable();

      const slider = fixture.debugElement.query(By.css('p-slider')).componentInstance;
      expect(slider.min).toBe(10);
      expect(slider.max).toBe(100);
    });
  });

  describe('Temperature Input Clamping', () => {
    let fixture: ComponentFixture<ThermostatGauge>;

    beforeEach(async () => {
      await TestBed.configureTestingModule({
        imports: [ThermostatGauge],
        providers: [provideNoopAnimations()]
      }).compileComponents();
      fixture = TestBed.createComponent(ThermostatGauge);
    });

    it('should have min 60 and max 80 on the temperature input', async () => {
      fixture.componentRef.setInput('name', 'Thermo');
      fixture.componentRef.setInput('location', 'Room');
      fixture.componentRef.setInput('desiredTemperature', 70);
      fixture.componentRef.setInput('ambientTemperature', 70);
      fixture.componentRef.setInput('mode', ThermostatMode.Auto);
      fixture.componentRef.setInput('state', ThermostatState.Idle);
      fixture.detectChanges();
      await fixture.whenStable();

      const inputNumber = fixture.debugElement.query(By.css('p-inputnumber')).componentInstance;
      expect(inputNumber.min).toBe(60);
      expect(inputNumber.max).toBe(80);
    });
  });

  describe('Device Registration Validation', () => {
    let component: RegisterDeviceDialog;
    let fixture: ComponentFixture<RegisterDeviceDialog>;
    let deviceApiMock: any;

    beforeEach(async () => {
      deviceApiMock = {
        registerDevice: vi.fn().mockReturnValue(of({}))
      };

      await TestBed.configureTestingModule({
        imports: [RegisterDeviceDialog],
        providers: [
          { provide: DeviceApiService, useValue: deviceApiMock },
          MessageService,
          provideNoopAnimations()
        ]
      }).compileComponents();

      fixture = TestBed.createComponent(RegisterDeviceDialog);
      component = fixture.componentInstance;
    });

    it('should require name, location, and type to enable submit', async () => {
      fixture.componentRef.setInput('visible', true);
      fixture.componentRef.setInput('existingLocations', []);
      fixture.detectChanges();
      await fixture.whenStable();

      const submitButton = fixture.debugElement.query(By.css('button[type="submit"]')).nativeElement as HTMLButtonElement;

      // Initially empty
      expect(submitButton.disabled).toBe(true);

      // Fill name
      component.name.set('New Device');
      fixture.detectChanges();
      expect(submitButton.disabled).toBe(true);

      // Fill location
      component.location.set('New Room');
      fixture.detectChanges();
      expect(submitButton.disabled).toBe(true);

      // Fill type
      component.type.set(DeviceType.Light);
      fixture.detectChanges();

      // Now it should be enabled (the component uses computed to check if form is valid)
      expect(submitButton.disabled).toBe(false);
    });
  });
});
