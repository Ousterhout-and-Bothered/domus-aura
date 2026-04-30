namespace SmartHome.Domain.Simulation;


/// <summary>
/// Central source of truth for which simulation speeds are permitted.
/// Injected wherever validation or enumeration is needed so the allowed
/// set can be swapped without changing consumers.
/// </summary>
public interface ISimulationSpeedRegistry
{
    IReadOnlySet<SimulationSpeed> AllowedSpeeds { get; }
    bool IsAllowed(SimulationSpeed speed);
}