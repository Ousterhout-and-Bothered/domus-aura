import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  SceneExecutionResponse,
  SceneRequest,
  SceneResponse,
} from '../models/scene';

/**
 * HTTP client for the /api/scenes endpoints.
 */
@Injectable({ providedIn: 'root' })
export class SceneApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/scenes`;

  /** GET /api/scenes — list all scenes. */
  getAll(): Observable<SceneResponse[]> {
    return this.http.get<SceneResponse[]>(this.baseUrl);
  }

  /** GET /api/scenes/{id}. */
  getById(id: string): Observable<SceneResponse> {
    return this.http.get<SceneResponse>(`${this.baseUrl}/${id}`);
  }

  /** POST /api/scenes — create. */
  create(req: SceneRequest): Observable<SceneResponse> {
    return this.http.post<SceneResponse>(this.baseUrl, req);
  }

  /** PUT /api/scenes/{id} — replace name + actions wholesale. */
  update(id: string, req: SceneRequest): Observable<SceneResponse> {
    return this.http.put<SceneResponse>(`${this.baseUrl}/${id}`, req);
  }

  /** DELETE /api/scenes/{id}. */
  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /**
   * POST /api/scenes/{id}/execute — run the scene.
   * Returns per-action outcomes; partial failures don't abort the batch.
   */
  execute(id: string): Observable<SceneExecutionResponse> {
    return this.http.post<SceneExecutionResponse>(
      `${this.baseUrl}/${id}/execute`,
      null
    );
  }
}
