using ThermostatDevice = SmartHome.Domain.Device.Thermostat.Thermostat;

namespace SmartHome.Domain.Device.Repository;

/// <summary>
/// Defines the persistence contract for smart home devices.
/// Provides asynchronous access to query, add, remove, and persist devices
/// without exposing infrastructure concerns such as EF Core to the domain or service layers.
/// Simulation-specific operations live on <see cref="Simulation.ISimulationRepository"/>.
/// </summary>
public interface IDeviceRepository
{
    /// <summary>
    /// Retrieves all devices, with optional filtering by location, type, and power state.
    /// </summary>
    Task<IReadOnlyList<Device>> GetAllAsync(
        string? location = null,
        DeviceType? type = null,
        bool? isOn = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a device by its unique identifier.
    /// Returns null when no matching device exists.
    /// </summary>
    Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a device by its unique identifier without enabling change tracking.
    /// Intended for read-only scenarios where no modifications will be applied to the entity.
    /// Improves query performance by avoiding EF Core tracking overhead.
    /// </summary>
    Task<Device?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new device to persistence.
    /// The change is not guaranteed to be committed until SaveChangesAsync is called.
    /// </summary>
    Task AddAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the device with the specified identifier.
    /// Returns true when a matching device was found and deleted; otherwise false.
    /// </summary>
    Task<bool> RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a thermostat already exists at the specified location.
    /// Used to enforce one thermostat per location.
    /// </summary>
    Task<bool> ThermostatExistsAtLocationAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all thermostats in the specified location.
    /// Returned instances are change-tracked.
    /// </summary>
    Task<IReadOnlyList<ThermostatDevice>> GetThermostatsByLocationAsync(
        string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the underlying storage medium.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if any device exists in the repository.
    /// </summary>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a command operation in the device's history.
    /// </summary>
    Task LogActionAsync(Guid deviceId, string operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the command history for a specific device, ordered by most recent first.
    /// </summary>
    Task<IReadOnlyList<CommandHistory>> GetHistoryAsync(Guid deviceId, CancellationToken cancellationToken = default);
}