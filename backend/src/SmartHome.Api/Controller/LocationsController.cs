using Microsoft.AspNetCore.Mvc;
using SmartHome.Infrastructure.Simulation;

namespace SmartHome.Api.Controllers;

/// <summary>
/// Provides API endpoints for managing location-based simulation settings.
/// </summary>
[ApiController]
[Route("api/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly ISimulationService _simulationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationsController"/> class.
    /// </summary>
    /// <param name="simulationService">
    /// The simulation service responsible for location-based environment updates.
    /// </param>
    public LocationsController(ISimulationService simulationService)
    {
        // Inject simulation service
        // Handles all environment and thermostat logic
        _simulationService = simulationService;
    }

    /// <summary>
    /// Sets the ambient temperature for a given location.
    /// Throws <see cref="ArgumentException"/> if the temperature is invalid.
    /// Throws <see cref="InvalidOperationException"/> if the location does not exist.
    /// </summary>
    /// <param name="location">The name of the location to update.</param>
    /// <param name="request">The request containing the new ambient temperature.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A <see cref="NoContentResult"/> if the update succeeds.</returns>
    [HttpPut("{location}/ambient-temperature")]
    public async Task<IActionResult> SetAmbientTemperature(
        string location,
        [FromBody] SetAmbientTemperatureRequest request,
        CancellationToken cancellationToken)
    {
        // Delegate to simulation service — controller remains thin and avoids business logic
        await _simulationService.SetAmbientTemperatureAsync(location, request.Temperature, cancellationToken);

        // Return 204 since the update succeeds without a response body
        return NoContent();
    }
}

/// <summary>
/// Represents a request to update the ambient temperature for a location.
/// </summary>
/// <param name="Temperature">The ambient temperature, in degrees Fahrenheit.</param>
public sealed record SetAmbientTemperatureRequest(int Temperature);