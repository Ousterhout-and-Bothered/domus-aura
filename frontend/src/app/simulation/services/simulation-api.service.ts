import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AllowedSpeedsResponse,
  SetAmbientTemperatureRequest,
  SetAmbientTemperatureResponse,
  SetSimulationSpeedRequest,
  SimulationStateResponse,
} from '../models/simulation';

/**
 * HTTP client for /api/simulation and /api/locations/{location}/ambient-temperature.
 */
@Injectable({ providedIn: 'root' })
export class SimulationApiService {
  private readonly http = inject(HttpClient);
  private readonly simBase = `${environment.apiUrl}/simulation`;
  private readonly locBase = `${environment.apiUrl}/locations`;

  /** GET /api/simulation — current speed and clock. */
  getState(): Observable<SimulationStateResponse> {
    return this.http.get<SimulationStateResponse>(this.simBase);
  }

  /** GET /api/simulation/allowed-speeds — permitted multipliers for the speed dropdown. */
  getAllowedSpeeds(): Observable<AllowedSpeedsResponse> {
    return this.http.get<AllowedSpeedsResponse>(`${this.simBase}/allowed-speeds`);
  }

  /** PUT /api/simulation/speed — change the simulation speed. */
  setSpeed(speed: number): Observable<void> {
    const body: SetSimulationSpeedRequest = { speedMultiplier: speed };
    return this.http.put<void>(`${this.simBase}/speed`, body);
  }

  /** POST /api/simulation/reset — reset all devices to defaults. */
  resetAllDevices(): Observable<void> {
    return this.http.post<void>(`${this.simBase}/reset`, null);
  }

  /**
   * PUT /api/locations/{location}/ambient-temperature.
   * Affects every thermostat at that location.
   */
  setAmbientTemperature(
    location: string,
    temperature: number
  ): Observable<SetAmbientTemperatureResponse> {
    const body: SetAmbientTemperatureRequest = { temperature };
    return this.http.put<SetAmbientTemperatureResponse>(
      `${this.locBase}/${encodeURIComponent(location)}/ambient-temperature`,
      body
    );
  }
}
