using SmartHome.Domain.Simulation;

namespace SmartHome.Infrastructure.Simulation.Clock;


/// <summary>
/// Owns the mutable simulation state (current time and speed multiplier).
/// Registered as a singleton so state persists across scoped service calls
/// and background ticks. All members are thread-safe.
/// </summary>
public interface ISimulationClock
{
    SimulationSpeed Speed { get; }
    DateTime CurrentTime { get; }

    void SetSpeed(SimulationSpeed speed);
    void Advance(TimeSpan simulatedInterval);
    void Reset();
}