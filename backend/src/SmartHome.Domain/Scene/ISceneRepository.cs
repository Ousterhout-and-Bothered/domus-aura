namespace SmartHome.Domain.Scene;

/// <summary>
/// Defines the persistence contract for <see cref="DeviceScene"/> aggregates.
/// Provides asynchronous access to query, add, update, and remove scenes
/// without exposing infrastructure concerns such as EF Core to the domain
/// or service layers.
/// </summary>
public interface ISceneRepository
{
    /// <summary>
    /// Retrieves all scenes.
    /// </summary>
    Task<IReadOnlyList<DeviceScene>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a scene by its unique identifier. Returns null when no matching scene exists.
    /// </summary>
    Task<DeviceScene?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new scene to persistence.
    /// </summary>
    Task AddAsync(DeviceScene scene, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing scene. The implementation must handle replacement of the
    /// action collection as a delete-and-reinsert inside a transaction, since
    /// <see cref="DeviceScene.ReplaceActions"/> discards the prior collection wholesale.
    /// </summary>
    Task UpdateAsync(DeviceScene scene, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the scene with the specified identifier. Returns true when a matching
    /// scene was found and deleted; otherwise false.
    /// </summary>
    Task<bool> RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes scene actions that target a specific device by ID.
    /// Group-based actions are not removed because they are resolved at execution time.
    /// Scenes left with no actions are removed.
    /// Returns the names of scenes that were affected.
    /// </summary>
    Task<IReadOnlyList<string>> RemoveActionsForDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Persists all pending changes to the underlying storage medium.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}