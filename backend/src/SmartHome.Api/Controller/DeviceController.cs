using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using SmartHome.Api.Contracts.Devices;
using SmartHome.Domain.Device;
using SmartHome.Infrastructure.Device.Events;
using SmartHome.Domain.Common;

namespace SmartHome.Api.Controller;

/// <summary>
/// API controller for managing smart home devices.
/// Provides endpoints for device registration, discovery, state control, and history retrieval.
/// </summary>
[ApiController]
[Route("api/devices")]
[Authorize]
public class DeviceController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IDeviceEventStream _deviceEventStream;
    private readonly IOptions<JsonOptions> _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceController"/> class.
    /// </summary>
    /// <param name="deviceService">The service responsible for device business logic and orchestration.</param>
    /// <param name="deviceEventStream">The event stream used to subscribe to real-time device change notifications (SSE).</param>
    /// <param name="jsonOptions">The configured JSON serialization options used when streaming event payloads.</param>
    public DeviceController(
        IDeviceService deviceService,
        IDeviceEventStream deviceEventStream,
        IOptions<JsonOptions> jsonOptions)
    {
        _deviceService = deviceService;
        _deviceEventStream = deviceEventStream;
        _jsonOptions = jsonOptions;
    }

    /// <summary>
    /// Retrieves all devices, with optional filtering by location, type, and power state.
    /// </summary>
    /// <param name="location">Optional filter by device location.</param>
    /// <param name="type">Optional filter by device type.</param>
    /// <param name="state">Optional filter by power state ("on" or "off"). (Note: Thermostats in Idle are considered off).</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A list of devices matching the specified filters.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Device>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<Device>>> GetAll(
        [FromQuery] string? location,
        [FromQuery] DeviceType? type,
        [FromQuery] string? state,
        CancellationToken cancellationToken)
    {
        bool? isOn = state?.ToLowerInvariant() switch
        {
            "on" => true,
            "off" => false,
            _ => null
        };

        var devices = await _deviceService.GetAllDevicesAsync(location, type, isOn, cancellationToken);
        return Ok(devices);
    }

    /// <summary>
    /// Retrieves a specific device by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the device.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The device details if found; otherwise, a 404 Not Found error.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Device>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var device = await _deviceService.GetDeviceByIdAsync(id, cancellationToken);
        return Ok(device);
    }

    /// <summary>
    /// Registers a new device in the system.
    /// </summary>
    /// <param name="request">The registration details.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The newly created device, or a 409 Conflict if a thermostat already exists at the location.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Device), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Device>> Create(
        [FromBody] RegisterDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var device = await _deviceService.RegisterDeviceAsync(
            request.Name, request.Location, request.Type, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
    }

    /// <summary>
    /// Removes a device from the system.
    /// </summary>
    /// <param name="id">The unique identifier of the device to remove.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>204 No Content on success, or 404 Not Found if the device does not exist.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _deviceService.RemoveDeviceAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Updates a device's editable metadata (name and location).
    /// Returns the updated device. If neither field changed, returns the device unchanged
    /// without writing to history.
    /// </summary>
    /// <param name="id">The unique identifier of the device.</param>
    /// <param name="request">The new name and location for the device.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The updated device, or 404 if not found, or 409 if a relocate would conflict with the thermostat-per-location rule.</returns>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Device>> Update(
        Guid id,
        [FromBody] UpdateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var device = await _deviceService.UpdateDeviceAsync(
            id, request.Name, request.Location, cancellationToken);

        return Ok(device);
    }

    /// <summary>
    /// Retrieves the command history for a specific device.
    /// </summary>
    /// <param name="id">The unique identifier of the device.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>An ordered list of previously executed commands.</returns>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IEnumerable<CommandHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CommandHistory>>> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var history = await _deviceService.GetDeviceHistoryAsync(id, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Retrieves a paged feed of command history across all devices, with optional filters.
    /// Ordered most recent first.
    /// </summary>
    /// <param name="page">1-indexed page number. Defaults to 1. Empty string and missing both yield 1.</param>
    /// <param name="pageSize">Entries per page. Defaults to 50, capped at 200.</param>
    /// <param name="location">Optional location filter (matches device's current location).</param>
    /// <param name="deviceId">Optional device filter.</param>
    /// <param name="from">Optional inclusive UTC lower bound on entry timestamp.</param>
    /// <param name="to">Optional inclusive UTC upper bound on entry timestamp.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A page of command history entries plus the total count across all pages.</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResult<CommandHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CommandHistory>>> GetAllHistory(
        [FromQuery] string? page = null,
        [FromQuery] string? pageSize = null,
        [FromQuery] string? location = null,
        [FromQuery] Guid? deviceId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        // Accept page and pageSize as strings so that empty values (?page=&pageSize=)
        // are treated the same as the parameter being absent. Binding directly to int
        // produces a confusing 400 from the model binder when a client sends an
        // empty string, even though "no value" is the intent in both cases.
        var resolvedPage = ParseIntOrDefault(page, defaultValue: 1, min: 1);
        var resolvedPageSize = ParseIntOrDefault(pageSize, defaultValue: 50, min: 1, max: 200);

        var result = await _deviceService.GetAllHistoryAsync(
            resolvedPage, resolvedPageSize, location, deviceId, from, to, cancellationToken);

        return Ok(result);
    }

    private static int ParseIntOrDefault(string? raw, int defaultValue, int min = int.MinValue, int max = int.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out var value))
        {
            return defaultValue;
        }

        return Math.Clamp(value, min, max);
    }

    /// <summary>
    /// Executes a state change command on a device (e.g., turn on/off, set brightness).
    /// </summary>
    /// <param name="id">The unique identifier of the device.</param>
    /// <param name="request">The command details (command name and optional value).</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The updated device state, or an error if the command is invalid for the device type.</returns>
    [HttpPut("{id:guid}/state")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Device>> UpdateState(
        Guid id,
        [FromBody] DeviceCommandRequest request,
        CancellationToken cancellationToken)
    {
        var device = await _deviceService.ExecuteCommandAsync(
            id, request.Command, request.Value?.ToString(), cancellationToken);

        return Ok(device);
    }

    /// <summary>
    /// Subscribes the client to a real-time stream of device state changes using Server-Sent Events (SSE).
    /// Acts as an HTTP adapter over the device event stream and does not contain business logic.
    /// </summary>
    [HttpGet("events")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task GetEvents(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        await foreach (var deviceEvent in _deviceEventStream.SubscribeAsync(cancellationToken))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                deviceEvent,
                _jsonOptions.Value.JsonSerializerOptions);

            await Response.WriteAsync("event: deviceChanged\n", cancellationToken);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}