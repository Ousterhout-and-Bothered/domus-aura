using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Contracts.Devices;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Repository;
using System.Text.Json;
using SmartHome.Infrastructure.Device.Events;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;

namespace SmartHome.Api.Controller;

/// <summary>
/// API controller for managing smart home devices.
/// Provides endpoints for device registration, discovery, state control, and history retrieval.
/// </summary>
[Authorize]
[ApiController]
[Route("api/devices")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDeviceService _deviceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceController"/> class.
    /// </summary>
    /// <param name="deviceRepository">The repository for device persistence and retrieval.</param>
    /// <param name="deviceService">The service for managing device business logic.</param>
    public DeviceController(
        IDeviceRepository deviceRepository,
        IDeviceService deviceService)
    {
        _deviceRepository = deviceRepository;
        _deviceService = deviceService;
    }

    /// <summary>
    /// Retrieves all devices, with optional filtering by location, type, and power state.
    /// </summary>
    /// <param name="location">Optional filter by device location.</param>
    /// <param name="type">Optional filter by device type.</param>
    /// <param name="state">Optional filter by power state ("on" or "off"). (Note: Thermostats in Idle are considered off).</param>
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

        var devices = await _deviceRepository.GetAllAsync(location, type, isOn, cancellationToken);
        return Ok(devices);
    }

    /// <summary>
    /// Retrieves a specific device by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the device.</param>
    /// <returns>The device details if found; otherwise, a 404 Not Found error.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Device>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(id, cancellationToken);

        if (device is null)
        {
            return DeviceNotFound(id);
        }

        return Ok(device);
    }

    /// <summary>
    /// Registers a new device in the system.
    /// </summary>
    /// <param name="request">The registration details.</param>
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
    /// Retrieves the command history for a specific device.
    /// </summary>
    /// <param name="id">The unique identifier of the device.</param>
    /// <returns>An ordered list of previously executed commands.</returns>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IEnumerable<CommandHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CommandHistory>>> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(id, cancellationToken);

        if (device is null)
        {
            return DeviceNotFound(id);
        }

        var history = await _deviceRepository.GetHistoryAsync(id, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Executes a state change command on a device (e.g., turn on/off, set brightness).
    /// </summary>
    /// <param name="id">The unique identifier of the device.</param>
    /// <param name="request">The command details (command name and optional value).</param>
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

    private ObjectResult DeviceNotFound(Guid id)
    {
        return Problem(
            type: "https://domus-aura.com/problems/device-not-found",
            title: "Device not found",
            detail: $"No device with id {id} exists.",
            statusCode: StatusCodes.Status404NotFound);
    }
    
    /// <summary>
    /// Subscribes the client to a real-time stream of device state changes using Server-Sent Events (SSE).
    /// </summary>
    /// <remarks>
    /// This endpoint establishes a long-lived HTTP connection that continuously streams events
    /// whenever any device state changes.
    ///
    /// Each event contains:
    /// - deviceId: the affected device
    /// - changeType: Created, Updated, or Deleted
    /// - payload: a snapshot of the device's current state
    ///
    /// The payload structure depends on the device type but always includes:
    /// - id
    /// - name
    /// - location
    /// - type
    ///
    /// Additional fields are included based on device type:
    /// - Light: powerState, brightness, colorHex
    /// - Fan: powerState, speed
    /// - Thermostat: state, mode, desiredTemperature, ambientTemperature
    /// - DoorLock: lockState
    ///
    /// Clients should use the browser's EventSource API to consume this stream.
    /// The browser automatically attempts to reconnect if the connection is interrupted.
    /// </remarks>
    /// <returns>
    /// A continuous text/event-stream connection. The response remains open and streams events over time.
    /// </returns>
    [HttpGet("events")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task GetEvents(
        [FromServices] IDeviceEventStream deviceEventStream,
        [FromServices] IOptions<JsonOptions> jsonOptions,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        await foreach (var deviceEvent in deviceEventStream.SubscribeAsync(cancellationToken))
        {
            var json = JsonSerializer.Serialize(deviceEvent, jsonOptions.Value.JsonSerializerOptions);

            await Response.WriteAsync("event: deviceChanged\n", cancellationToken);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}