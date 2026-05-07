import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';

interface ChatRequestBody {
  message: string;
}

interface ChatResponseBody {
  response: string;
}

/**
 * HTTP client for the /api/chat endpoint. The backend takes a single
 * user message, dispatches it through the LLM with tool-calling, and
 * returns a plain-text reply. Device mutations the LLM performs as a
 * side effect propagate to the rest of the UI via the existing SSE
 * stream — this service does not need to coordinate with the device
 * list at all.
 */
@Injectable({ providedIn: 'root' })
export class ChatApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/chat`;

  /**
   * POST /api/chat — send a single user message, get the LLM's reply
   * as a plain string. Errors propagate as HttpErrorResponse for the
   * caller to handle.
   */
  sendMessage(message: string): Observable<string> {
    const body: ChatRequestBody = { message };
    return this.http
      .post<ChatResponseBody>(this.baseUrl, body)
      .pipe(map(res => res.response));
  }
}
