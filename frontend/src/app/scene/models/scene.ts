import { DeviceType } from '../../device/models/device';

/* ─────────────── Requests ─────────────── */

export interface SceneActionRequest {
  /** Set when targeting one specific device. Mutually exclusive with deviceType. */
  deviceId?: string | null;
  /** Set when targeting a group. Mutually exclusive with deviceId. */
  deviceType?: DeviceType | null;
  /** Optional location scope for a group target. Null matches any location. */
  location?: string | null;
  /** Command name (camelCase): setPower, setBrightness, setColor, lock, unlock, etc. */
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

export interface SceneExecutionEntryResponse {
  deviceId: string;
  operation: string;
  success: boolean;
  message?: string | null;
}

export interface SceneExecutionResponse {
  sceneId: string;
  sceneName: string;
  succeededCount: number;
  failedCount: number;
  entries: SceneExecutionEntryResponse[];
}
