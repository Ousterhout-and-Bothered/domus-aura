using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device.Events;
using SmartHome.Domain.Common;

namespace SmartHome.Domain.Device;

/// <summary>
/// Service for coordinating high-level device operations and business rules.
/// Acts as a bridge between the API and the domain/persistence layers.
/// </summary>
public interface IDeviceService
{
    /// <summary>
    /// Registers a new device in the system after validating business rules.
    /// </summary>
    /// <param name="name">The name of the device.</param>
    /// <param name="location">The location of the device.</param>
    /// <param name="type">The type of device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created device.</returns>
    /// <exception cref="DuplicateThermostatException">
    /// Thrown if a thermostat is registered in a location that already has one.
    /// </exception>
    /// <remarks>
    /// On success, a <see cref="DeviceChangeType.Created"/> event is published
    /// so connected clients can update in real time via SSE.
    /// </remarks>
    Task<Device> RegisterDeviceAsync(string name, string location, DeviceType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command on a device and logs the action.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device.</param>
    /// <param name="commandName">The name of the command to execute.</param>
    /// <param name="value">The optional value for the command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated device.</returns>
    /// <exception cref="ResourceNotFoundException">Thrown if the device is not found.</exception>
    /// <remarks>
    /// Applies the command, persists the updated device state, and publishes an
    /// <see cref="DeviceChangeType.Updated"/> event so connected SSE clients can stay synchronized.
    /// </remarks>
    Task<Device> ExecuteCommandAsync(Guid deviceId, string commandName, string? value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a device from the system.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ResourceNotFoundException">Thrown if the device is not found.</exception>
    /// <remarks>
    /// Persists the removal and publishes a <see cref="DeviceChangeType.Deleted"/> event
    /// so connected SSE clients can remove the device from their UI in real time.
    /// </remarks>
    Task RemoveDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);


    /// <summary>
    /// Updates a device's editable metadata (name and location).
    /// Detects which fields actually changed and logs only the deltas to command history.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device.</param>
    /// <param name="name">The new name for the device.</param>
    /// <param name="location">The new location for the device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated device.</returns>
    /// <exception cref="ResourceNotFoundException">Thrown if the device is not found.</exception>
    /// <exception cref="DuplicateThermostatException">
    /// Thrown if relocating a thermostat to a location that already has one.
    /// </exception>
    /// <remarks>
    /// When neither field changes, no audit entry is written and no event is published —
    /// the call is a no-op that returns the unchanged device. When at least one field
    /// changes, a single combined audit entry is written and a
    /// <see cref="DeviceChangeType.Updated"/> event is published so connected SSE clients
    /// can refresh.
    /// </remarks>
    Task<Device> UpdateDeviceAsync(
        Guid deviceId,
        string name,
        string location,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Retrieves all devices, optionally filtered by location, type, and power state.
    /// </summary>
    /// <param name="location">
    /// Optional location filter. When provided, only devices in the specified location are returned.
    /// </param>
    /// <param name="type">
    /// Optional device type filter. When provided, only devices of the specified type are returned.
    /// </param>
    /// <param name="isOn">
    /// Optional power-state filter. When <c>true</c>, only devices considered "on" are returned.
    /// When <c>false</c>, only powered devices that are off are returned. When <c>null</c>, no state filtering is applied.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A read-only list of devices matching the provided filters.
    /// </returns>
    /// <remarks>
    /// This method coordinates device retrieval and filtering at the service layer,
    /// ensuring the API remains decoupled from persistence concerns. Latch devices
    /// (e.g., door locks) are always considered "on" for filtering purposes.
    /// </remarks>
    Task<IReadOnlyList<Device>> GetAllDevicesAsync(
        string? location,
        DeviceType? type,
        bool? isOn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paged feed of command history across all devices, with optional filters.
    /// Ordered most recent first.
    /// </summary>
    /// <param name="page">1-indexed page number.</param>
    /// <param name="pageSize">Entries per page.</param>
    /// <param name="location">Optional location filter.</param>
    /// <param name="deviceId">Optional device filter.</param>
    /// <param name="from">Optional inclusive UTC lower bound on entry timestamp.</param>
    /// <param name="to">Optional inclusive UTC upper bound on entry timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page of matching command history entries plus the total count.</returns>
    Task<PagedResult<CommandHistory>> GetAllHistoryAsync(
        int page,
        int pageSize,
        string? location = null,
        Guid? deviceId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a device by its unique identifier.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching device.</returns>
    /// <exception cref="ResourceNotFoundException">Thrown if the device is not found.</exception>
    Task<Device> GetDeviceByIdAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the command history for a device after verifying the device exists.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The command history entries for the device.</returns>
    /// <exception cref="ResourceNotFoundException">Thrown if the device is not found.</exception>
    Task<IReadOnlyList<CommandHistory>> GetDeviceHistoryAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);
}