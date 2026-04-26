using SmartHome.Domain.Device.Commands;

namespace SmartHome.Domain.Scene;

/// <summary>
/// The outcome of executing a <see cref="DeviceScene"/>: per-action entries plus summary statistics.
/// </summary>
/// <param name="SceneId">The scene that was executed.</param>
/// <param name="SceneName">The scene's name at the time of execution.</param>
/// <param name="Entries">
/// One entry per leaf command executed, in execution order. Each entry pairs the
/// target device with the command's result (success or failure with message).
/// </param>
public sealed record SceneExecutionResult(
    Guid SceneId,
    string SceneName,
    IReadOnlyList<SceneExecutionEntry> Entries)
{
    /// <summary>The number of entries whose command succeeded.</summary>
    public int SucceededCount => Entries.Count(e => e.Result.Success);

    /// <summary>The number of entries whose command failed.</summary>
    public int FailedCount => Entries.Count(e => !e.Result.Success);
}

/// <summary>
/// A single entry in a <see cref="SceneExecutionResult"/>: the device a command targeted
/// and the result that command produced.
/// </summary>
public sealed record SceneExecutionEntry(
    Guid DeviceId,
    CommandResult Result);