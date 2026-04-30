using SmartHome.Domain.Simulation;

namespace SmartHome.Api.Contracts;

/// <summary>
/// Snapshot of the simulation's current state.
/// </summary>
/// <param name="SpeedMultiplier">The active simulation speed multiplier.</param>
/// <param name="SimulationClock">The current simulation time (UTC).</param>
public sealed record SimulationStateResponse(
    int SpeedMultiplier,
    DateTime SimulationClock);