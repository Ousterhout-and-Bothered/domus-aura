/**
 * Permitted simulation speed multipliers. Matches Domain/Simulation/SimulationSpeed.cs.
 * The backend returns the actual permitted set from /api/simulation/allowed-speeds.
 */
export type SimulationSpeed = 1 | 2 | 5 | 10;

export interface SimulationStateResponse {
  speedMultiplier: number;
  /** ISO 8601 UTC timestamp. */
  simulationClock: string;
}

export interface AllowedSpeedsResponse {
  speeds: number[];
}

export interface SetSimulationSpeedRequest {
  speedMultiplier: number;
}

export interface SetAmbientTemperatureRequest {
  temperature: number;
}

export interface SetAmbientTemperatureResponse {
  location: string;
  ambientTemperature: number;
}
