import { DeviceType } from '../../device/models/device';

/* ─────────────── Requests ─────────────── */

export interface SceneActionRequest {
  /** Set when targeting one specific device. Mutually exclusive with deviceType. */
  deviceId?: string | null;
  /** Set when targeting a group. Mutually exclusive with deviceId. */
  deviceType?: DeviceType | null;
  /** Optional location scope for a group target. Null matches any location. */
  location?: string | null;
  /** Command name (e.g., SetPower, SetBrightness, SetColor, Lock, Unlock). */
  operation: string;
  /** Stringified value. Null for parameterless operations. */
  value?: string | null;

}

export interface SceneRequest {
  name: string;
  actions: SceneActionRequest[];

}

/* ─────────────── Responses ─────────────── */

export interface SceneActionResponse {
  id: string;
  deviceId?: string | null;
  deviceType?: DeviceType | null;
  location?: string | null;
  operation: string;
  value?: string | null;
  orderIndex: number;
}

export interface SceneResponse {
  id: string;
  name: string;
  actions: SceneActionResponse[];
}

/**
 * Per-action outcome from a scene execution. The backend reports status
 * as a string ("succeeded" / "failed") rather than a boolean — this
 * leaves room for additional terminal states (e.g., "skipped") later
 * without a breaking contract change.
 */
export interface SceneExecutionResultResponse {
  orderIndex: number;
  deviceId: string;
  deviceName: string;
  deviceType: DeviceType;
  operation: string;
  value: unknown | null;
  status: string;
  message?: string | null;
  implicitPowerOn?: boolean;
  implicitModeChange?: boolean;
}

export interface SceneExecutionSummaryResponse {
  succeeded: number;
  failed: number;
}

export interface SceneExecutionResponse {
  sceneId: string;
  sceneName: string;
  summary: SceneExecutionSummaryResponse;
  results: SceneExecutionResultResponse[];
}
