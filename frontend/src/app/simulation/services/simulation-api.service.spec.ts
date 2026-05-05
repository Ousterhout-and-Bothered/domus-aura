import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SimulationApiService } from './simulation-api.service';
import { environment } from '../../../environments/environment';

describe('SimulationApiService', () => {
  let service: SimulationApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SimulationApiService]
    });
    service = TestBed.inject(SimulationApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get simulation state', () => {
    const dummyState = { speedMultiplier: 1, simulationClock: new Date().toISOString() };
    service.getState().subscribe(state => {
      expect(state.speedMultiplier).toBe(1);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/simulation`);
    expect(req.request.method).toBe('GET');
    req.flush(dummyState);
  });

  it('should get allowed speeds', () => {
    const dummySpeeds = { speeds: [1, 2, 5, 10] };
    service.getAllowedSpeeds().subscribe(response => {
      expect(response.speeds).toEqual([1, 2, 5, 10]);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/simulation/allowed-speeds`);
    expect(req.request.method).toBe('GET');
    req.flush(dummySpeeds);
  });

  it('should set speed', () => {
    service.setSpeed(5).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/simulation/speed`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ speedMultiplier: 5 });
    req.flush(null);
  });

  it('should reset all devices', () => {
    service.resetAllDevices().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/simulation/reset`);
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });

  it('should set ambient temperature', () => {
    const dummyResponse = { location: 'Kitchen', ambientTemperature: 75 };
    service.setAmbientTemperature('Kitchen', 75).subscribe(res => {
      expect(res.ambientTemperature).toBe(75);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/locations/Kitchen/ambient-temperature`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ temperature: 75 });
    req.flush(dummyResponse);
  });
});
