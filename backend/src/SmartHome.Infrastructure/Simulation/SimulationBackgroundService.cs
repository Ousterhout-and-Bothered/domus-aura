using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartHome.Domain.Simulation;
using SmartHome.Infrastructure.Simulation.Clock;

namespace SmartHome.Infrastructure.Simulation;

/// <summary>
/// Runs the simulation loop in the background.
/// Creates a fresh DI scope for each tick so the hosted service can safely
/// resolve the scoped <see cref="ISimulationService"/> and its scoped dependencies.
/// </summary>
/// <param name="scopeFactory">
/// Creates service scopes for resolving scoped simulation services during each tick.
/// </param>
/// <param name="clock">
/// Provides the current simulation speed and base tick interval used to schedule the loop.
/// </param>
public sealed class SimulationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ISimulationClock clock) : BackgroundService
{

    /// <summary>
    /// Executes the background simulation loop until the host is shutting down.
    /// </summary>
    /// <param name="stoppingToken">
    /// Signals when the host is stopping and the background loop should exit.
    /// </param>
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

                var multiplier = Math.Max(1, (int)clock.Speed);
                var delay = clock.BaseTickInterval / multiplier;

                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown - graceful exit.
        }
    }
}