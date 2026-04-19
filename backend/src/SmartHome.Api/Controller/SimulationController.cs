using Microsoft.AspNetCore.Mvc;
using SmartHome.Infrastructure.Simulation;

namespace SmartHome.Api.Controllers;

/// <summary>
/// Provides API endpoints for managing global simulation settings,
/// including simulation speed, clock state, and system reset operations.
/// </summary>
[ApiController]
[Route("api/simulation")]
public sealed class SimulationController : ControllerBase
{
    private readonly ISimulationService _simulationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationController"/> class.
    /// </summary>
    /// <param name="simulationService">
    /// The simulation service responsible for managing simulation state and behavior.
    /// </param>
    public SimulationController(ISimulationService simulationService)
    {
        // Inject simulation service
        // Centralizes simulation logic and state
        _simulationService = simulationService;
    }

    /// <summary>
    /// Retrieves the current simulation state, including speed multiplier and simulation clock.
    /// </summary>
    /// <returns>
    /// An object containing the current simulation speed multiplier and simulation clock value.
    /// </returns>
    [HttpGet]
    public IActionResult GetSimulationState() =>
        Ok(new
        {
            // Expose current simulation speed
            // Controls thermostat tick rate
            speedMultiplier = _simulationService.SpeedMultiplier,

            // Expose simulation clock
            // Used for UI display and time tracking
            simulationClock = _simulationService.SimulationClock
        });

    /// <summary>
    /// Sets the simulation speed multiplier.
    /// Throws <see cref="ArgumentException"/> if the speed multiplier is invalid.
    /// </summary>
    /// <param name="request">The request containing the new speed multiplier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A <see cref="NoContentResult"/> if the update succeeds.</returns>
    [HttpPut("speed")]
    public async Task<IActionResult> SetSpeed([FromBody] SetSimulationSpeedRequest request, CancellationToken cancellationToken)
    {
        // Delegate to simulation service
        // Enforces valid speed values and applies change
        await _simulationService.SetSpeedAsync(request.SpeedMultiplier, cancellationToken);

        // Return 204 since the update succeeds without a response body
        return NoContent();
    }

    /// <summary>
    /// Resets all devices in the simulation to their default states.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A <see cref="NoContentResult"/> if the reset succeeds.</returns>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken)
    {
        // Reset all devices
        // Returns system to factory defaults per specification
        await _simulationService.ResetAllDevicesAsync(cancellationToken);

        // Return 204 since the operation completes without a response body
        return NoContent();
    }
}

/// <summary>
/// Represents a request to update the simulation speed multiplier.
/// </summary>
/// <param name="SpeedMultiplier">
/// The simulation speed multiplier (e.g., 1x, 2x, 5x, 10x).
/// </param>
public sealed record SetSimulationSpeedRequest(int SpeedMultiplier);