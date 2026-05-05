import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { DeviceApiService } from './device-api.service';
import { environment } from '../../../environments/environment';
import { AnyDevice, FanSpeed } from '../models/device-types';
import { DeviceType, PowerState } from '../models/device';

describe('DeviceApiService', () => {
  let service: DeviceApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [DeviceApiService]
    });
    service = TestBed.inject(DeviceApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get all devices', () => {
    const dummyDevices: AnyDevice[] = [
      { id: '1', name: 'Light', location: 'Living Room', type: DeviceType.Light, $type: DeviceType.Light, powerState: PowerState.On, brightness: 100, colorHex: '#FFFFFF' }
    ];

    service.getAll().subscribe(devices => {
      expect(devices.length).toBe(1);
      expect(devices).toEqual(dummyDevices);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/devices`);
    expect(req.request.method).toBe('GET');
    req.flush(dummyDevices);
  });

  it('should get device by id', () => {
    const dummyDevice: AnyDevice = { id: '1', name: 'Light', location: 'Living Room', type: DeviceType.Light, $type: DeviceType.Light, powerState: PowerState.On, brightness: 100, colorHex: '#FFFFFF' };

    service.getById('1').subscribe(device => {
      expect(device).toEqual(dummyDevice);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/devices/1`);
    expect(req.request.method).toBe('GET');
    req.flush(dummyDevice);
  });

  it('should register a device', () => {
    const newDevice: AnyDevice = { id: '2', name: 'Fan', location: 'Bedroom', type: DeviceType.Fan, $type: DeviceType.Fan, powerState: PowerState.Off, speed: FanSpeed.Medium };
    const regReq = { name: 'Fan', location: 'Bedroom', type: DeviceType.Fan };

    service.register(regReq).subscribe(device => {
      expect(device).toEqual(newDevice);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/devices`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(regReq);
    req.flush(newDevice);
  });

  it('should remove a device', () => {
    service.remove('1').subscribe(response => {
      expect(response).toBeNull();
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/devices/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('should execute a command', () => {
    const updatedDevice: AnyDevice = { id: '1', name: 'Light', location: 'Living Room', type: DeviceType.Light, $type: DeviceType.Light, powerState: PowerState.Off, brightness: 100, colorHex: '#FFFFFF' };
    const commandReq = { command: 'TurnOff' };

    service.executeCommand('1', commandReq).subscribe(device => {
      expect(device).toEqual(updatedDevice);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/devices/1/state`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(commandReq);
    req.flush(updatedDevice);
  });

  it('should get history', () => {
    const dummyHistory = [{ id: 'h1', deviceId: '1', operation: 'TurnOn', timestamp: new Date() }];

    service.getHistory('1').subscribe(history => {
      expect(history.length).toBe(1);
      expect(history[0].operation).toBe('TurnOn');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/devices/1/history`);
    expect(req.request.method).toBe('GET');
    req.flush(dummyHistory);
  });
});
