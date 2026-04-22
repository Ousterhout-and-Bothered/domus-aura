using SmartHome.Domain.Simulation;

namespace SmartHome.Infrastructure.Simulation.Clock;

public sealed class SimulationClock(ISimulationSpeedRegistry speedRegistry) : ISimulationClock
{
    private readonly Lock _lock = new();
    private SimulationSpeed _speed = SimulationSpeed.X1;
    private DateTime _currentTime = DateTime.UtcNow;

    public SimulationSpeed Speed
    {
        get
        {
            lock (_lock) return _speed; 
        }
    }

    public DateTime CurrentTime
    {
        get
        {
            lock (_lock) return _currentTime;
        } 
    }

    public TimeSpan BaseTickInterval { get; } = TimeSpan.FromSeconds(5);

    public void SetSpeed(SimulationSpeed speed)
    {
        if (!speedRegistry.IsAllowed(speed))
            throw new ArgumentException(
                $"Speed '{(int)speed}' is not allowed. Permitted speeds: " +
                $"{string.Join(", ", speedRegistry.AllowedSpeeds.Select(s => (int)s).Order())}.");

        lock (_lock) _speed = speed;
    }

    public void Advance(TimeSpan simulatedInterval)
    {
        lock (_lock) _currentTime = _currentTime.Add(simulatedInterval);
    }

    public void Reset()
    {
        lock (_lock) _currentTime = DateTime.UtcNow;
    }
}