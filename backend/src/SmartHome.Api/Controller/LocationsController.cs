using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Domain.Simulation;

namespace SmartHome.Api.Controller;

/// <summary>
/// Provides API endpoints for managing location-based simulation settings.
/// </summary>
[ApiController]
[Route("api/locations")]
[Produces("application/json")]
public sealed class LocationsController(ISimulationService simulationService) : ControllerBase
{
    /// <summary>
    /// Sets the ambient temperature for a given location.
    /// Applies to every thermostat at that location.
    /// </summary>
    /// <param name="location">The name of the location to update.</param>
    /// <param name="request">The request containing the new ambient temperature.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <response code="204">Ambient temperature successfully updated.</response>
    /// <response code="400">Temperature out of range or malformed request.</response>
    /// <response code="409">No thermostats exist at the specified location.</response>
    [HttpPut("{location}/ambient-temperature")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetAmbientTemperature(
        string location,
        [FromBody] SetAmbientTemperatureRequest request,
        CancellationToken cancellationToken)
    {
        await simulationService.SetAmbientTemperatureAsync(location, request.Temperature, cancellationToken);
        return NoContent();
    }
}

/// <summary>
/// Request to update the ambient temperature for a location.
/// </summary>
/// <param name="Temperature">The ambient temperature in degrees Fahrenheit.</param>
public sealed record SetAmbientTemperatureRequest(int Temperature);