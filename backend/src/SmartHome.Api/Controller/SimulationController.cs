using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Contracts;
using SmartHome.Domain.Common;
using SmartHome.Domain;
using SmartHome.Domain.Simulation;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace SmartHome.Api.Controller;

/// <summary>
/// Provides API endpoints for managing global simulation settings,
/// including simulation speed, clock state, and system reset operations.
/// </summary>
/// <param name="simulationService">The service responsible for global simulation logic.</param>
[Authorize]
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
            (int)simulationService.Speed,
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
        Ok(new AllowedSpeedsResponse(registry.AllowedSpeeds.Select(s => (int)s).Order().ToList()));

    /// <summary>
    /// Sets the simulation speed.
    /// </summary>
    /// <param name="request">The desired simulation speed multiplier.</param>
    /// <response code="204">Speed successfully updated.</response>
    /// <response code="400">The requested speed is invalid, or passed incorrectly in the URL instead of the body.</response>
    [HttpPut("speed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetSpeed(
        [FromBody] SetSimulationSpeedRequest request,
        CancellationToken cancellationToken)
    {
        var speedValue = request.GetSpeedValue()!.Value;
        var speed = (SimulationSpeed)speedValue;

        await simulationService.SetSpeedAsync(speed, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Catches accidental path-based speed updates to provide a helpful error message.
    /// </summary>
    [HttpPut("speed/{*multiplier}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult RejectPathSpeed(string? multiplier)
    {
        return Problem(
            type: "https://domus-aura.com/problems/invalid-request-format",
            title: "Invalid Request Format",
            detail: $"The simulation speed '{multiplier}' was passed in the URL. Please send the speed multiplier in the JSON request body as '{{ \"speedMultiplier\": {multiplier} }}' instead.",
            statusCode: StatusCodes.Status400BadRequest);
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
/// Uses object? for SpeedMultiplier to allow custom validation messages when non-numeric types are passed.
/// </summary>
/// <param name="SpeedMultiplier">
/// The target simulation speed multiplier. Supports multipliers 1, 2, 5, or 10. (Must be an integer).
/// <example>5</example>
/// </param>
public sealed record SetSimulationSpeedRequest(object? SpeedMultiplier)
{
    /// <summary>
    /// Helper method to extract the integer value from the SpeedMultiplier property.
    /// Handles both direct numeric types and JsonElement values.
    /// </summary>
    /// <returns>The integer value if valid; otherwise null.</returns>
    public int? GetSpeedValue() => ValueParser.TryParseInt(SpeedMultiplier);
}