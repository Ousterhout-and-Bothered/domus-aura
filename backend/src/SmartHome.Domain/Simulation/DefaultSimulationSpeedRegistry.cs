namespace SmartHome.Domain.Simulation;

public sealed class DefaultSimulationSpeedRegistry : ISimulationSpeedRegistry
{
    public IReadOnlySet<SimulationSpeed> AllowedSpeeds { get; } =
        new HashSet<SimulationSpeed>
        {
            SimulationSpeed.Normal,
            SimulationSpeed.Double,
            SimulationSpeed.Fast,
            SimulationSpeed.Ultra
        };

    public bool IsAllowed(SimulationSpeed speed) => AllowedSpeeds.Contains(speed);
}