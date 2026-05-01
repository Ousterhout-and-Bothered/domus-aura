using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Device.Events;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Scene;


namespace SmartHome.Infrastructure.Scene;

/// <summary>
/// Default implementation of <see cref="ISceneService"/>. Coordinates scene persistence,
/// resolution, and execution while logging per-action results to device command history.
/// </summary>
public sealed class SceneService(
    ISceneRepository sceneRepository,
    IDeviceRepository deviceRepository,
    ISceneResolver resolver,
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

    /// <inheritdoc />
    public async Task<SceneExecutionResult> ExecuteSceneAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // 1. Load the scene (throws ResourceNotFoundException if missing)
        var scene = await GetSceneAsync(id, cancellationToken);

        // 2. Resolve the scene: expand groups, build the composite command tree
        var resolved = await resolver.ResolveAsync(scene, cancellationToken);

        // 3. Execute the composite synchronously. Partial failures are captured as
        //    failed CommandResults; execution does not abort on a failing child.
        var results = resolved.Composite.Execute();

        // 4. Pair each result with the device it targeted (via the parallel id list)
        //    and log to the device command history. Each log entry is scoped to the
        //    device that was affected, so "show me this device's history" naturally
        //    includes scene-triggered actions.
        var entries = new List<SceneExecutionEntry>(results.Count);
        for (var i = 0; i < results.Count; i++)
        {
            var deviceId = resolved.DeviceIdsInOrder[i];
            var result = results[i];

            if (deviceId != Guid.Empty)
            {
                // Encode success/failure into the label so device history doesn't
                // silently report a failed scene action as if it had succeeded.
                // The structured success bit lives on SceneExecutionEntry; this
                // string is the human-readable trace.
                var operationLabel = result.Success
                    ? $"{result.Operation} (scene: {scene.Name})"
                    : $"{result.Operation} [FAILED: {result.Message}] (scene: {scene.Name})";

                await deviceRepository.LogActionAsync(deviceId, operationLabel, cancellationToken);
            }

            entries.Add(new SceneExecutionEntry(
                deviceId,
                result,
                resolved.OrderIndexesInOrder[i]));
        }

        // 5. Save all pending changes in one go: the device state mutations from
        //    command execution AND the history records just logged. Both sets of
        //    changes live in the same EF change tracker, so one SaveChangesAsync
        //    flushes both atomically.
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