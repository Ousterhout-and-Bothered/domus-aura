using SmartHome.Domain.Simulation;

namespace SmartHome.Api.Contracts;

/// <summary>
/// Snapshot of the simulation's current state.
/// </summary>
/// <param name="Speed">The active simulation speed.</param>
/// <param name="SimulationClock">The current simulation time (UTC).</param>
public sealed record SimulationStateResponse(
    SimulationSpeed Speed,
    DateTime SimulationClock);