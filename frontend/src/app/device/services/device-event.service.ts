import { Injectable, OnDestroy, signal, inject, NgZone } from '@angular/core';
import { environment } from '../../../environments/environment';
import { DeviceChangedEvent } from '../models/device';

/**
 * Subscribes to /api/devices/events (SSE) and exposes the latest event as a signal.
 *
 * Usage in a component:
 *   private events = inject(DeviceEventService);
 *   constructor() {
 *     effect(() => {
 *       const evt = this.events.lastEvent();
 *       if (evt) this.applyChange(evt);
 *     });
 *   }
 *
 * The native EventSource auto-reconnects on transient errors, so we don't need
 * manual reconnection logic. We DO need to push the event into NgZone so
 * change detection runs — EventSource fires outside Angular's zone.
 */
@Injectable({ providedIn: 'root' })
export class DeviceEventService implements OnDestroy {
  private readonly zone = inject(NgZone);
  private source: EventSource | null = null;

  /** Latest event received, or null if no event has arrived yet. */
  readonly lastEvent = signal<DeviceChangedEvent | null>(null);

  /** Connection status, useful for showing a "live" indicator in the UI. */
  readonly connected = signal(false);

  /** Open the SSE connection. Idempotent — calling twice is a no-op. */
  connect(): void {
    if (this.source) return;

    const url = `${environment.apiUrl}/devices/events`;
    this.source = new EventSource(url);

    this.source.addEventListener('deviceChanged', (e) => {
      const data = JSON.parse((e as MessageEvent).data) as DeviceChangedEvent;
      // EventSource callbacks fire outside Angular's zone — wrap to trigger CD.
      this.zone.run(() => this.lastEvent.set(data));
    });

    this.source.addEventListener('open', () => {
      this.zone.run(() => this.connected.set(true));
    });

    this.source.addEventListener('error', () => {
      // Browser auto-reconnects; just reflect the disconnected state.
      this.zone.run(() => this.connected.set(false));
    });
  }

  /** Close the SSE connection. */
  disconnect(): void {
    this.source?.close();
    this.source = null;
    this.connected.set(false);
  }

  ngOnDestroy(): void {
    this.disconnect();
  }
}
