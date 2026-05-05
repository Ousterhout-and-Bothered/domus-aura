import { Injectable, OnDestroy, signal, inject, NgZone } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DeviceChangedEvent } from '../models/device';
import { AuthService } from '../../authentication/service/auth.service';

/**
 * Subscribes to the `/api/devices/events` Server-Sent Events (SSE) endpoint.
 * Exposes incoming device state changes as an Observable.
 */
@Injectable({ providedIn: 'root' })
export class DeviceEventService implements OnDestroy {
  private readonly zone = inject(NgZone);
  private readonly auth = inject(AuthService);
  private source: EventSource | null = null;
  private subscriberCount = 0;

  private readonly _events$ = new Subject<DeviceChangedEvent>();
<<<<<<< Updated upstream
  readonly events$: Observable<DeviceChangedEvent> = this._events$.asObservable();
=======

  /**
   * Observable stream of device change events.
   */
  readonly events$: Observable<DeviceChangedEvent> = this._events$.asObservable();

  /**
   * Signal indicating the current connection status to the SSE endpoint.
   */
>>>>>>> Stashed changes
  readonly connected = signal(false);

  /**
   * Establishes a connection to the device events SSE endpoint.
   * If a connection already exists, this method does nothing.
   */
  connect(): void {
    this.subscriberCount++;
    if (this.source) return;

    const token = this.auth.getAccessToken();
    if (!token) {
      // No token yet — caller should call connect() after auth completes.
      return;
    }

    const url = `${environment.apiUrl}/devices/events?access_token=${encodeURIComponent(token)}`;
    this.source = new EventSource(url);

    this.source.addEventListener('deviceChanged', (e) => {
      const data = JSON.parse((e as MessageEvent).data) as DeviceChangedEvent;
      console.log('[SSE] deviceChanged:', data.deviceId, data.changeType);
      this.zone.run(() => this._events$.next(data));
    });

    this.source.addEventListener('deviceChanged', (e) => {
      const data = JSON.parse((e as MessageEvent).data) as DeviceChangedEvent;
      console.log('[SSE] deviceChanged FULL:', JSON.stringify(data));
      this.zone.run(() => this._events$.next(data));
    });

    this.source.addEventListener('error', () => {
      this.zone.run(() => this.connected.set(false));
    });
  }

  /**
   * Closes the connection to the device events SSE endpoint and updates the status.
   */
  disconnect(): void {
    this.subscriberCount = Math.max(0, this.subscriberCount - 1);
    if (this.subscriberCount > 0) return;

    this.source?.close();
    this.source = null;
    this.connected.set(false);
  }

  ngOnDestroy(): void {
    this.subscriberCount = 0;
    this.source?.close();
    this.source = null;
    this.connected.set(false);
    // Note: deliberately NOT completing _events$ — completion is permanent
    // and breaks any future subscribers if the service is reused via HMR.
  }
}
