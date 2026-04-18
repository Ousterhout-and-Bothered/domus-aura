using Microsoft.Extensions.Hosting;

namespace SmartHome.Infrastructure.Simulation;

public sealed class ThermostatSimulationBackgroundService : BackgroundService
{
    private readonly ISimulationService _simulationService;

    public ThermostatSimulationBackgroundService(ISimulationService simulationService)
    {
        // Inject simulation service
        // Drives thermostat behavior over time
        _simulationService = simulationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Continuously run simulation loop until application shuts down
        while (!stoppingToken.IsCancellationRequested)
        {
            // AUpdates thermostat states and temperatures
            await _simulationService.TickAsync(stoppingToken);

            // Calculate delay based on simulation speed multiplier
            var delay = TimeSpan.FromSeconds(5.0 / _simulationService.SpeedMultiplier);

            // Wait before next tick
            await Task.Delay(delay, stoppingToken);
        }
    }
}