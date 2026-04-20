using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartHome.Domain.Simulation;
using SmartHome.Infrastructure.Simulation.Clock;

namespace SmartHome.Infrastructure.Simulation;

/// <summary>
/// Drives the simulation loop in the background. Creates a fresh DI scope
/// per tick so it can resolve the scoped ISimulationService (and its scoped
/// repository/DbContext) safely from this singleton hosted service.
/// </summary>
public sealed class SimulationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ISimulationClock clock) : BackgroundService
{
    private static readonly TimeSpan BaseRealTimePerTick = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var simulationService = scope.ServiceProvider
                        .GetRequiredService<ISimulationService>();

                    await simulationService.TickAsync(stoppingToken);
                }

                var multiplier = (int)clock.Speed;
                var delay = BaseRealTimePerTick / multiplier;

                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown — the host is stopping. Nothing to do.
        }
    }
}