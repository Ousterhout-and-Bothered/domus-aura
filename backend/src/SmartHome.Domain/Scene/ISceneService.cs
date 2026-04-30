using SmartHome.Domain.Common.Exceptions;

namespace SmartHome.Domain.Scene;

/// <summary>
/// Service for coordinating high-level scene operations: CRUD, execution, and
/// audit logging. Acts as a bridge between the API and the domain/persistence layers.
/// </summary>
public interface ISceneService
{
    /// <summary>
    /// Creates and persists a new scene.
    /// </summary>
    Task<DeviceScene> CreateSceneAsync(
        string name,
        IEnumerable<SceneAction> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a scene by its unique identifier.
    /// </summary>
    /// <exception cref="ResourceNotFoundException">Thrown if no matching scene exists.</exception>
    Task<DeviceScene> GetSceneAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all scenes.
    /// </summary>
    Task<IReadOnlyList<DeviceScene>> GetAllScenesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing scene's name and action list. The existing action list is
    /// replaced wholesale with the provided one.
    /// </summary>
    /// <exception cref="ResourceNotFoundException">Thrown if no matching scene exists.</exception>
    Task<DeviceScene> UpdateSceneAsync(
        Guid id,
        string newName,
        IEnumerable<SceneAction> newActions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the scene with the specified identifier.
    /// </summary>
    /// <exception cref="ResourceNotFoundException">Thrown if no matching scene exists.</exception>
    Task DeleteSceneAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a scene by resolving its actions into commands, running them in order,
    /// and recording each outcome in the device command history. Individual command
    /// failures do not abort the batch — per-action results are reported in the returned value.
    /// </summary>
    /// <exception cref="ResourceNotFoundException">Thrown if no matching scene exists.</exception>
    Task<SceneExecutionResult> ExecuteSceneAsync(Guid id, CancellationToken cancellationToken = default);
}