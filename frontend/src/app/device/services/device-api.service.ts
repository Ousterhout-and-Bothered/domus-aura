import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CommandHistory,
  DeviceCommandRequest,
  DeviceFilters,
  HistoryFilters,
  PagedResult,
  RegisterDeviceRequest,
  UpdateDeviceRequest,
} from '../models/device';
import { AnyDevice } from '../models/device-types';

/**
 * HTTP client for the /api/devices endpoints.
 * Thin wrapper — no business logic, no caching. Components should subscribe
 * directly or compose with state libraries (signals, etc.) at the call site.
 */
@Injectable({ providedIn: 'root' })
export class DeviceApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/devices`;

  /** GET /api/devices — list with optional filters. */
  getAll(filters: DeviceFilters = {}): Observable<AnyDevice[]> {
    let params = new HttpParams();
    if (filters.location) params = params.set('location', filters.location);
    if (filters.type) params = params.set('type', filters.type);
    if (filters.state) params = params.set('state', filters.state);
    return this.http.get<AnyDevice[]>(this.baseUrl, { params });
  }

  /** GET /api/devices/{id}. Throws 404 if missing. */
  getById(id: string): Observable<AnyDevice> {
    return this.http.get<AnyDevice>(`${this.baseUrl}/${id}`);
  }

  /** POST /api/devices — register a new device. 409 if a thermostat already exists at the location. */
  register(req: RegisterDeviceRequest): Observable<AnyDevice> {
    return this.http.post<AnyDevice>(this.baseUrl, req);
  }

  /** PATCH /api/devices/{id} — update editable metadata (name, location). 409 if a relocate would conflict with the thermostat-per-location rule. */
  update(id: string, req: UpdateDeviceRequest): Observable<AnyDevice> {
    return this.http.patch<AnyDevice>(`${this.baseUrl}/${id}`, req);
  }

  /** DELETE /api/devices/{id}. */
  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** PUT /api/devices/{id}/state — execute a command. */
  executeCommand(id: string, req: DeviceCommandRequest): Observable<AnyDevice> {
    return this.http.put<AnyDevice>(`${this.baseUrl}/${id}/state`, req);
  }

  /** GET /api/devices/{id}/history — chronological list of past commands. */
  getHistory(id: string): Observable<CommandHistory[]> {
    return this.http.get<CommandHistory[]>(`${this.baseUrl}/${id}/history`);
  }

  /** GET /api/devices/history — paged feed of command history across all devices. */
  getAllHistory(filters: HistoryFilters = {}): Observable<PagedResult<CommandHistory>> {
    let params = new HttpParams();

    if (filters.page !== undefined) params = params.set('page', filters.page);
    if (filters.pageSize !== undefined) params = params.set('pageSize', filters.pageSize);
    if (filters.location) params = params.set('location', filters.location);
    if (filters.deviceId) params = params.set('deviceId', filters.deviceId);
    if (filters.from) params = params.set('from', filters.from);
    if (filters.to) params = params.set('to', filters.to);

    return this.http.get<PagedResult<CommandHistory>>(`${this.baseUrl}/history`, { params });
  }
}
