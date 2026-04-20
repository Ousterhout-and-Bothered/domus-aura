using SmartHome.Domain.Simulation;

namespace SmartHome.Infrastructure.Simulation.Clock;

public sealed class SimulationClock(ISimulationSpeedRegistry speedRegistry) : ISimulationClock
{
    private readonly Lock _lock = new();
    private SimulationSpeed _speed = SimulationSpeed.Normal;
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
    
    public void SetSpeed(SimulationSpeed speed)
    {
        if (!speedRegistry.IsAllowed(speed))
            throw new ArgumentOutOfRangeException(
                nameof(speed),
                $"Speed '{speed}' is not allowed. Permitted speeds: " +
                $"{string.Join(", ", speedRegistry.AllowedSpeeds)}.");

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