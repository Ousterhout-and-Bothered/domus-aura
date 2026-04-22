using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Domain.Common;
using SmartHome.Domain;
using SmartHome.Domain.Simulation;
using System.Text.Json;

namespace SmartHome.Api.Controller;

/// <summary>
/// Provides API endpoints for managing location-based simulation settings.
/// </summary>
/// <param name="simulationService">The service responsible for location-wide simulation logic.</param>
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
    /// <response code="200">Ambient temperature successfully updated.</response>
    /// <response code="400">Temperature out of range or malformed request.</response>
    /// <response code="409">No thermostats exist at the specified location.</response>
    [HttpPut("{location}/ambient-temperature")]
    [ProducesResponseType(typeof(SetAmbientTemperatureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SetAmbientTemperatureResponse>> SetAmbientTemperature(
        string location,
        [FromBody] SetAmbientTemperatureRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentException("The set ambient temperature request body is missing or in an improper format.");
        }

        var temperatureValue = request.GetTemperatureValue();
        if (temperatureValue == null)
        {
            throw new ArgumentException("The temperature must be a valid number.");
        }

        await simulationService.SetAmbientTemperatureAsync(location, temperatureValue.Value, cancellationToken);
        return Ok(new SetAmbientTemperatureResponse(location, temperatureValue.Value));
    }
}

/// <summary>
/// Request to update the ambient temperature for a location.
/// Uses object? for Temperature to allow custom validation messages when non-numeric types are passed.
/// </summary>
/// <param name="Temperature">
/// The ambient temperature in degrees Fahrenheit. (Must be an integer).
/// <example>72</example>
/// </param>
public sealed record SetAmbientTemperatureRequest(object? Temperature)
{
    /// <summary>
    /// Helper method to extract the integer value from the Temperature property.
    /// Handles both direct numeric types and JsonElement values.
    /// </summary>
    /// <returns>The integer value if valid; otherwise null.</returns>
    public int? GetTemperatureValue() => ValueParser.TryParseInt(Temperature);
}

/// <summary>
/// Response returned after successfully updating the ambient temperature.
/// </summary>
/// <param name="Location">The name of the location that was updated.</param>
/// <param name="AmbientTemperature">The new ambient temperature value.</param>
public sealed record SetAmbientTemperatureResponse(string Location, int AmbientTemperature);