import { Injectable, OnDestroy, signal, inject, NgZone } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DeviceChangedEvent } from '../models/device';

/**
 * Subscribes to /api/devices/events (SSE) and exposes incoming events as
 * an Observable. Each event is delivered exactly once to every subscriber.
 *
 * Usage in a component:
 *   private events = inject(DeviceEventService);
 *   constructor() {
 *     this.events.events$
 *       .pipe(takeUntilDestroyed())
 *       .subscribe((evt) => this.applyChange(evt));
 *   }
 */
@Injectable({ providedIn: 'root' })
export class DeviceEventService implements OnDestroy {
  private readonly zone = inject(NgZone);
  private source: EventSource | null = null;

  private readonly _events$ = new Subject<DeviceChangedEvent>();

  readonly events$: Observable<DeviceChangedEvent> = this._events$.asObservable();

  readonly connected = signal(false);

  connect(): void {
    if (this.source) return;

    const url = `${environment.apiUrl}/devices/events`;
    this.source = new EventSource(url);

    this.source.addEventListener('deviceChanged', (e) => {
      const data = JSON.parse((e as MessageEvent).data) as DeviceChangedEvent;
      this.zone.run(() => this._events$.next(data));
    });

    this.source.addEventListener('open', () => {
      this.zone.run(() => this.connected.set(true));
    });

    this.source.addEventListener('error', () => {
      this.zone.run(() => this.connected.set(false));
    });
  }

  disconnect(): void {
    this.source?.close();
    this.source = null;
    this.connected.set(false);
  }

  ngOnDestroy(): void {
    this.disconnect();
    this._events$.complete();
  }
}
