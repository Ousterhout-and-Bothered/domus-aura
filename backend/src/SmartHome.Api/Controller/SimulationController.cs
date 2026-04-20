using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Contracts;
using SmartHome.Domain.Simulation;

namespace SmartHome.Api.Controller;

/// <summary>
/// Provides API endpoints for managing global simulation settings,
/// including simulation speed, clock state, and system reset operations.
/// </summary>
[ApiController]
[Route("api/simulation")]
[Produces("application/json")]
public sealed class SimulationController(ISimulationService simulationService) : ControllerBase
{
    /// <summary>
    /// Retrieves the current simulation state — active speed and simulation clock.
    /// </summary>
    /// <response code="200">The current simulation state.</response>
    [HttpGet]
    [ProducesResponseType(typeof(SimulationStateResponse), StatusCodes.Status200OK)]
    public ActionResult<SimulationStateResponse> GetSimulationState() =>
        Ok(new SimulationStateResponse(
            simulationService.Speed,
            simulationService.SimulationClock));

    /// <summary>
    /// Lists the speeds permitted by the current simulation speed registry.
    /// Frontend dropdowns can consume this endpoint instead of hardcoding the list.
    /// </summary>
    /// <response code="200">The set of permitted simulation speeds.</response>
    [HttpGet("allowed-speeds")]
    [ProducesResponseType(typeof(AllowedSpeedsResponse), StatusCodes.Status200OK)]
    public ActionResult<AllowedSpeedsResponse> GetAllowedSpeeds(
        [FromServices] ISimulationSpeedRegistry registry) =>
        Ok(new AllowedSpeedsResponse(registry.AllowedSpeeds.ToList()));

    /// <summary>
    /// Sets the simulation speed.
    /// </summary>
    /// <response code="204">Speed successfully updated.</response>
    /// <response code="400">The requested speed is invalid or not permitted by the registry.</response>
    [HttpPut("speed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetSpeed(
        [FromBody] SetSimulationSpeedRequest request,
        CancellationToken cancellationToken)
    {
        await simulationService.SetSpeedAsync(request.Speed, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Resets all devices in the simulation to their default states and resets the simulation clock.
    /// </summary>
    /// <response code="204">All devices successfully reset.</response>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken)
    {
        await simulationService.ResetAllDevicesAsync(cancellationToken);
        return NoContent();
    }
}

/// <summary>
/// Request to update the simulation speed.
/// </summary>
/// <param name="Speed">The target simulation speed. Must be permitted by the registry.</param>
public sealed record SetSimulationSpeedRequest(SimulationSpeed Speed);