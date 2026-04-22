namespace SmartHome.Domain.Simulation;

public sealed class DefaultSimulationSpeedRegistry : ISimulationSpeedRegistry
{
    public IReadOnlySet<SimulationSpeed> AllowedSpeeds { get; } =
        new HashSet<SimulationSpeed>
        {
            SimulationSpeed.X1,
            SimulationSpeed.X2,
            SimulationSpeed.X5,
            SimulationSpeed.X10
        };

    public bool IsAllowed(SimulationSpeed speed) => AllowedSpeeds.Contains(speed);
}