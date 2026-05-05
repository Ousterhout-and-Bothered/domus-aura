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
 * HTTP client for simulation control and ambient temperature adjustments.
 */
@Injectable({ providedIn: 'root' })
export class SimulationApiService {
  private readonly http = inject(HttpClient);
  private readonly simBase = `${environment.apiUrl}/simulation`;
  private readonly locBase = `${environment.apiUrl}/locations`;

  /**
   * Retrieves the current simulation state, including speed and clock.
   *
   * @returns An observable of the simulation state response.
   */
  getState(): Observable<SimulationStateResponse> {
    return this.http.get<SimulationStateResponse>(this.simBase);
  }

  /**
   * Fetches the list of allowed simulation speeds.
   *
   * @returns An observable containing the permitted speed multipliers.
   */
  getAllowedSpeeds(): Observable<AllowedSpeedsResponse> {
    return this.http.get<AllowedSpeedsResponse>(`${this.simBase}/allowed-speeds`);
  }

  /**
   * Changes the speed of the simulation.
   *
   * @param speed - The new simulation speed multiplier.
   * @returns An observable that completes when the speed is updated.
   */
  setSpeed(speed: number): Observable<void> {
    const body: SetSimulationSpeedRequest = { speedMultiplier: speed };
    return this.http.put<void>(`${this.simBase}/speed`, body);
  }

  /**
   * Resets all devices to their default states.
   *
   * @returns An observable that completes when the reset is finished.
   */
  resetAllDevices(): Observable<void> {
    return this.http.post<void>(`${this.simBase}/reset`, null);
  }

  /**
   * Updates the ambient temperature for a specific location.
   * This affects the behavior of thermostats in that location.
   *
   * @param location - The name of the location to update.
   * @param temperature - The new ambient temperature value.
   * @returns An observable of the response after setting the temperature.
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
