using SmartHome.Domain.Common.Exceptions;

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
    Task<Device> ExecuteCommandAsync(Guid deviceId, string commandName, string? value, CancellationToken cancellationToken = default);
}
