import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LightBulb } from './light-bulb/light-bulb';
import { FanSpinning } from './fan-spinning/fan-spinning';
import { ThermostatGauge } from './thermostat-gauge/thermostat-gauge';
import { DoorLock } from './door-lock/door-lock';
import { PowerState, DeviceType } from '../models/device';
import { FanSpeed, ThermostatMode, ThermostatState, DoorLockState } from '../models/device-types';
import { By } from '@angular/platform-browser';
import { provideNoopAnimations } from '@angular/platform-browser/animations';

describe('Component Rendering Tests', () => {

  describe('LightBulb Rendering', () => {
    let component: LightBulb;
    let fixture: ComponentFixture<LightBulb>;

    beforeEach(async () => {
      await TestBed.configureTestingModule({
        imports: [LightBulb],
        providers: [provideNoopAnimations()]
      }).compileComponents();

      fixture = TestBed.createComponent(LightBulb);
      component = fixture.componentInstance;
    });

    it('should render correctly when On', async () => {
      fixture.componentRef.setInput('name', 'Living Room Light');
      fixture.componentRef.setInput('location', 'Living Room');
      fixture.componentRef.setInput('powerState', PowerState.On);
      fixture.componentRef.setInput('brightness', 80);
      fixture.componentRef.setInput('colorHex', '#FF0000');

      fixture.detectChanges();
      await fixture.whenStable();

      const brightnessSlider = fixture.debugElement.query(By.css('p-slider'));
      const colorPicker = fixture.debugElement.query(By.css('p-colorpicker'));

      expect(brightnessSlider).toBeTruthy();
      expect(colorPicker).toBeTruthy();
      expect(brightnessSlider.componentInstance.disabled).toBe(false);
      expect(colorPicker.componentInstance.disabled).toBe(false);
    });

    it('should render correctly when Off', async () => {
      fixture.componentRef.setInput('name', 'Living Room Light');
      fixture.componentRef.setInput('location', 'Living Room');
      fixture.componentRef.setInput('powerState', PowerState.Off);
      fixture.componentRef.setInput('brightness', 80);
      fixture.componentRef.setInput('colorHex', '#FF0000');

      fixture.detectChanges();
      await fixture.whenStable();

      const brightnessSlider = fixture.debugElement.query(By.css('p-slider'));
      const colorPicker = fixture.debugElement.query(By.css('p-colorpicker'));

      expect(brightnessSlider.componentInstance.disabled).toBe(true);
      expect(colorPicker.componentInstance.disabled).toBe(true);
    });
  });

  describe('FanSpinning Rendering', () => {
    let component: FanSpinning;
    let fixture: ComponentFixture<FanSpinning>;

    beforeEach(async () => {
      await TestBed.configureTestingModule({
        imports: [FanSpinning],
        providers: [provideNoopAnimations()]
      }).compileComponents();

      fixture = TestBed.createComponent(FanSpinning);
      component = fixture.componentInstance;
    });

    it('should render correctly when On', async () => {
      fixture.componentRef.setInput('name', 'Ceiling Fan');
      fixture.componentRef.setInput('location', 'Bedroom');
      fixture.componentRef.setInput('powerState', PowerState.On);
      fixture.componentRef.setInput('speed', FanSpeed.Medium);

      fixture.detectChanges();
      await fixture.whenStable();

      const speedSelect = fixture.debugElement.query(By.css('p-selectbutton'));
      expect(speedSelect).toBeTruthy();
      expect(speedSelect.componentInstance.disabled).toBe(false);

      // Check if medium speed is active
      expect(speedSelect.componentInstance.value).toBe(FanSpeed.Medium);
    });

    it('should render correctly when Off', async () => {
      fixture.componentRef.setInput('name', 'Ceiling Fan');
      fixture.componentRef.setInput('location', 'Bedroom');
      fixture.componentRef.setInput('powerState', PowerState.Off);
      fixture.componentRef.setInput('speed', FanSpeed.Medium);

      fixture.detectChanges();
      await fixture.whenStable();

      const speedSelect = fixture.debugElement.query(By.css('p-selectbutton'));
      expect(speedSelect.componentInstance.disabled).toBe(true);
    });
  });

  describe('ThermostatGauge Rendering', () => {
    let component: ThermostatGauge;
    let fixture: ComponentFixture<ThermostatGauge>;

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
      it(`should render correctly in ${label} state`, async () => {
        fixture.componentRef.setInput('name', 'Main Thermostat');
        fixture.componentRef.setInput('location', 'Hallway');
        fixture.componentRef.setInput('desiredTemperature', 72);
        fixture.componentRef.setInput('ambientTemperature', 70);
        fixture.componentRef.setInput('mode', ThermostatMode.Auto);
        fixture.componentRef.setInput('state', state);

        fixture.detectChanges();
        await fixture.whenStable();

        const stateLabel = fixture.debugElement.query(By.css('.gauge-state')).nativeElement.textContent.trim();
        expect(stateLabel).toBe(label);

        const targetTemp = fixture.debugElement.query(By.css('.gauge-temp-big')).nativeElement.textContent.trim();
        expect(targetTemp).toBe('72');

        const ambientTemp = fixture.debugElement.query(By.css('.ambient-readout strong')).nativeElement.textContent.trim();
        expect(ambientTemp).toBe('70°F');

        const modeButtons = fixture.debugElement.query(By.css('.mode-select')).componentInstance;
        expect(modeButtons.value).toBe(ThermostatMode.Auto);
      });
    });
  });

  describe('DoorLock Rendering', () => {
    let component: DoorLock;
    let fixture: ComponentFixture<DoorLock>;

    beforeEach(async () => {
      await TestBed.configureTestingModule({
        imports: [DoorLock],
        providers: [provideNoopAnimations()]
      }).compileComponents();

      fixture = TestBed.createComponent(DoorLock);
      component = fixture.componentInstance;
    });

    it('should render correctly in Locked state', async () => {
      fixture.componentRef.setInput('name', 'Front Door');
      fixture.componentRef.setInput('location', 'Entry');
      fixture.componentRef.setInput('lockState', DoorLockState.Locked);

      fixture.detectChanges();
      await fixture.whenStable();

      const stateLabel = fixture.debugElement.query(By.css('.lock-state-label')).nativeElement.textContent.trim();
      expect(stateLabel).toBe('Locked');

      const toggle = fixture.debugElement.query(By.css('p-toggleswitch')).componentInstance;
      expect(toggle.ngModel).toBe(true); // isLocked() is true
    });

    it('should render correctly in Unlocked state', async () => {
      fixture.componentRef.setInput('name', 'Front Door');
      fixture.componentRef.setInput('location', 'Entry');
      fixture.componentRef.setInput('lockState', DoorLockState.Unlocked);

      fixture.detectChanges();
      await fixture.whenStable();

      const stateLabel = fixture.debugElement.query(By.css('.lock-state-label')).nativeElement.textContent.trim();
      expect(stateLabel).toBe('Unlocked');

      const toggle = fixture.debugElement.query(By.css('p-toggleswitch')).componentInstance;
      expect(toggle.ngModel).toBe(false); // isLocked() is false
    });
  });
});
