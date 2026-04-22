using SmartHome.Domain.Device;

namespace SmartHome.Domain.Simulation;

/// <summary>
/// Persistence operations specific to the simulation engine.
/// Focused interface: consumers that only need CRUD (e.g., device management)
/// should depend on <see cref="SmartHome.Domain.Device.Repository.IDeviceRepository"/>
/// instead of taking on simulation concerns they do not use.
/// </summary>
public interface ISimulationRepository
{
    /// <summary>
    /// Retrieves all devices that participate in simulation ticks.
    /// Returned instances are change-tracked and will be persisted
    /// on the next save.
    /// </summary>
    Task<IReadOnlyList<ITickable>> GetTickableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all devices to their default state.
    /// The change is not guaranteed to be committed until SaveChangesAsync is called.
    /// </summary>
    Task ResetAllAsync(CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Persists all pending simulation-related changes to the underlying storage medium.
    /// Must share a unit of work with <see cref="GetTickableAsync"/> so that
    /// mutations to returned entities are captured when this is called.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}