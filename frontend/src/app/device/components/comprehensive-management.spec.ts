import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RegisterDeviceDialog } from './register-device-dialog/register-device-dialog';
import { DeviceCard } from './device-card/device-card';
import { DeviceType, PowerState } from '../models/device';
import { By } from '@angular/platform-browser';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { DeviceApiService } from '../services/device-api.service';
import { MessageService, ConfirmationService } from 'primeng/api';
import { of } from 'rxjs';

describe('Comprehensive Device Management Tests', () => {

  describe('Register Device Form Submission', () => {
    let component: RegisterDeviceDialog;
    let fixture: ComponentFixture<RegisterDeviceDialog>;
    let deviceApiMock: any;

    beforeEach(async () => {
      deviceApiMock = {
        registerDevice: vi.fn().mockReturnValue(of({
          id: 'new-id',
          name: 'New Light',
          location: 'Kitchen',
          type: DeviceType.Light,
          powerState: PowerState.Off,
          brightness: 100,
          colorHex: '#FFFFFF'
        }))
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

    it('should submit form and emit new device', async () => {
      const createdSpy = vi.spyOn(component.deviceCreated, 'emit');

      fixture.componentRef.setInput('visible', true);
      fixture.componentRef.setInput('existingLocations', []);
      fixture.detectChanges();

      component.name.set('New Light');
      component.location.set('Kitchen');
      component.type.set(DeviceType.Light);
      fixture.detectChanges();

      const form = fixture.debugElement.query(By.css('form'));
      form.triggerEventHandler('ngSubmit', null);

      expect(deviceApiMock.registerDevice).toHaveBeenCalledWith({
        name: 'New Light',
        location: 'Kitchen',
        type: DeviceType.Light
      });
      expect(createdSpy).toHaveBeenCalledWith(expect.objectContaining({ id: 'new-id' }));
    });
  });

  describe('Remove Device Confirmation', () => {
    let component: DeviceCard;
    let fixture: ComponentFixture<DeviceCard>;
    let deviceApiMock: any;
    let confirmationService: ConfirmationService;

    beforeEach(async () => {
      deviceApiMock = {
        removeDevice: vi.fn().mockReturnValue(of(null))
      };

      await TestBed.configureTestingModule({
        imports: [DeviceCard],
        providers: [
          { provide: DeviceApiService, useValue: deviceApiMock },
          MessageService,
          ConfirmationService,
          provideNoopAnimations()
        ]
      }).compileComponents();

      fixture = TestBed.createComponent(DeviceCard);
      component = fixture.componentInstance;
      confirmationService = TestBed.inject(ConfirmationService);
    });

    it('should show confirmation dialog on remove click', async () => {
      const confirmSpy = vi.spyOn(confirmationService, 'confirm');

      fixture.componentRef.setInput('device', {
        id: '1',
        name: 'Test Light',
        location: 'Room',
        type: DeviceType.Light,
        powerState: PowerState.Off,
        brightness: 100,
        colorHex: '#FFFFFF'
      });
      fixture.detectChanges();

      const removeBtn = fixture.debugElement.query(By.css('.device-card-remove'));
      removeBtn.nativeElement.click();

      expect(confirmSpy).toHaveBeenCalled();
      const callArgs = confirmSpy.mock.calls[0][0];
      expect(callArgs.header).toContain('Remove Test Light');
    });

    it('should call delete on confirm', async () => {
      vi.spyOn(confirmationService, 'confirm').mockImplementation((options: any) => {
        return options.accept();
      });

      fixture.componentRef.setInput('device', {
        id: '1',
        name: 'Test Light',
        location: 'Room',
        type: DeviceType.Light,
        powerState: PowerState.Off,
        brightness: 100,
        colorHex: '#FFFFFF'
      });
      fixture.detectChanges();

      const removeBtn = fixture.debugElement.query(By.css('.device-card-remove'));
      removeBtn.nativeElement.click();

      expect(deviceApiMock.removeDevice).toHaveBeenCalledWith('1');
    });

    it('should NOT call delete on cancel', async () => {
      vi.spyOn(confirmationService, 'confirm').mockImplementation((options: any) => {
        if (options.reject) options.reject();
        return;
      });

      fixture.componentRef.setInput('device', {
        id: '1',
        name: 'Test Light',
        location: 'Room',
        type: DeviceType.Light,
        powerState: PowerState.Off,
        brightness: 100,
        colorHex: '#FFFFFF'
      });
      fixture.detectChanges();

      const removeBtn = fixture.debugElement.query(By.css('.device-card-remove'));
      removeBtn.nativeElement.click();

      expect(deviceApiMock.removeDevice).not.toHaveBeenCalled();
    });
  });
});
