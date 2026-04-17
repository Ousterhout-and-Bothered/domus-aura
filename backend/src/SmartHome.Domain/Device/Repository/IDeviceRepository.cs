using SmartHome.Domain.Device;

namespace SmartHome.Domain.Device.Repository;

/// <summary>
/// Defines the persistence contract for smart home devices.
/// Provides asynchronous access to query, add, remove, and persist devices
/// without exposing infrastructure concerns such as EF Core to the domain or service layers.
/// </summary>
public interface IDeviceRepository
{
    /// <summary>
    /// Retrieves all devices.
    /// </summary>
    Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a device by its unique identifier.
    /// Returns null when no matching device exists.
    /// </summary>
    Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new device to persistence.
    /// The change is not guaranteed to be committed until SaveChangesAsync is called.
    /// </summary>
    Task AddAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the device with the specified identifier.
    /// Returns true when a device was found and marked for removal; otherwise false.
    /// The change is not guaranteed to be committed until SaveChangesAsync is called.
    /// </summary>
    Task<bool> RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a thermostat already exists at the specified location.
    /// Used to enforce one thermostat per location.
    /// </summary>
    Task<bool> ThermostatExistsAtLocationAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the underlying storage medium.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}