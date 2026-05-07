using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Device.Events;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Scene;

namespace SmartHome.Infrastructure.Scene;

/// <summary>
/// Default implementation of <see cref="ISceneService"/>.
/// Responsible for managing scene lifecycle operations (create, update, delete)
/// and coordinating scene execution, including normalization, resolution,
/// command execution, and device history logging.
/// </summary>
/// <param name="sceneRepository">
/// Repository used to persist and retrieve scene definitions.
/// </param>
/// <param name="deviceRepository">
/// Repository used to persist device state changes and command history entries.
/// </param>
/// <param name="resolver">
/// Resolves a scene definition into executable commands by expanding device groups
/// and constructing a composite command tree.
/// </param>
/// <param name="sceneActionNormalizer">
/// Preprocesses scene actions to enforce execution constraints such as ordering
/// and prerequisite insertion (e.g., ensuring devices are powered on before applying changes).
/// </param>
public sealed class SceneService(
    ISceneRepository sceneRepository,
    IDeviceRepository deviceRepository,
    ISceneResolver resolver,
    ISceneActionNormalizer sceneActionNormalizer,
    IDeviceEventNotifier eventNotifier) : ISceneService
{
    /// <inheritdoc />
    public async Task<DeviceScene> CreateSceneAsync(
        string name,
        IEnumerable<SceneAction> actions,
        CancellationToken cancellationToken = default)
    {
        var scene = new DeviceScene(name, actions);

        await sceneRepository.AddAsync(scene, cancellationToken);
        await sceneRepository.SaveChangesAsync(cancellationToken);

        return scene;
    }

    /// <inheritdoc />
    public async Task<DeviceScene> GetSceneAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await sceneRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException($"Scene with id {id} not found.");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeviceScene>> GetAllScenesAsync(CancellationToken cancellationToken = default) =>
        sceneRepository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<DeviceScene> UpdateSceneAsync(
        Guid id,
        string newName,
        IEnumerable<SceneAction> newActions,
        CancellationToken cancellationToken = default)
    {
        var scene = await GetSceneAsync(id, cancellationToken);

        scene.Rename(newName);
        scene.ReplaceActions(newActions);

        await sceneRepository.UpdateAsync(scene, cancellationToken);
        await sceneRepository.SaveChangesAsync(cancellationToken);

        return scene;
    }

    /// <inheritdoc />
    public async Task DeleteSceneAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var removed = await sceneRepository.RemoveByIdAsync(id, cancellationToken);

        if (!removed)
        {
            throw new ResourceNotFoundException($"No scene with id {id} exists.");
        }

        await sceneRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Executes a scene by normalizing its actions, resolving them into executable commands,
    /// executing those commands, and logging the results to device history.
    /// </summary>
    /// <param name="id">The unique identifier of the scene to execute.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="SceneExecutionResult"/> containing per-action results,
    /// including success/failure status and execution ordering.
    /// </returns>
    /// <exception cref="ResourceNotFoundException">
    /// Thrown if no scene exists with the specified identifier.
    /// </exception>
    public async Task<SceneExecutionResult> ExecuteSceneAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var scene = await GetSceneAsync(id, cancellationToken);

        var executableScene = sceneActionNormalizer.Normalize(scene);

        var resolved = await resolver.ResolveAsync(executableScene, cancellationToken);

        var results = resolved.Composite.Execute();

        var entries = new List<SceneExecutionEntry>(results.Count);
        for (var i = 0; i < results.Count; i++)
        {
            var deviceId = resolved.DeviceIdsInOrder[i];
            var result = results[i];

            if (deviceId != Guid.Empty)
            {
                var operationDescription = result.Value is null
                    ? result.Operation
                    : $"{result.Operation}: {result.Value}";

                var operationLabel = result.Success
                    ? $"{operationDescription} (scene: {scene.Name})"
                    : $"{operationDescription} [FAILED: {result.Message}] (scene: {scene.Name})";

                await deviceRepository.LogActionAsync(deviceId, operationLabel, cancellationToken);
            }

            entries.Add(new SceneExecutionEntry(
                deviceId,
                result,
                resolved.OrderIndexesInOrder[i]));
        }

        await deviceRepository.SaveChangesAsync(cancellationToken);

        var children = resolved.Composite.Children;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.DeviceId == Guid.Empty) continue;
            if (!entry.Result.Success) continue;

            if (children[i] is DeviceCommandBase command)
            {
                await eventNotifier.PublishAsync(
                    command.Device,
                    DeviceChangeType.Updated,
                    cancellationToken);
            }
        }

        return new SceneExecutionResult(scene.Id, scene.Name, entries);
    }
}