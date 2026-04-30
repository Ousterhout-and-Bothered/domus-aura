using SmartHome.Domain.Device.Commands;

namespace SmartHome.Domain.Scene;

/// <summary>
/// The outcome of executing a <see cref="DeviceScene"/>: per-command entries plus summary statistics.
/// </summary>
/// <param name="SceneId">The scene that was executed.</param>
/// <param name="SceneName">The scene's name at the time of execution.</param>
/// <param name="Entries">
/// One entry per executed command, in execution order. Each entry pairs the
/// target device with the command's result and preserves the originating
/// scene action's order index.
/// </param>
/// <remarks>
/// Entries are generated after group resolution and command execution.
/// A single scene action may produce multiple entries when it targets a group.
/// Each entry retains the originating <see cref="SceneAction.OrderIndex"/> so
/// results can be traced back to the original scene definition.
/// </remarks>
public sealed record SceneExecutionResult(
    Guid SceneId,
    string SceneName,
    IReadOnlyList<SceneExecutionEntry> Entries)
{
    /// <summary>The number of entries whose command succeeded (including no-ops).</summary>
    public int SucceededCount => Entries.Count(e => e.Result.Success);

    /// <summary>The number of entries whose command failed.</summary>
    public int FailedCount => Entries.Count(e => !e.Result.Success);
}

/// <summary>
/// A single entry in a <see cref="SceneExecutionResult"/> representing the outcome
/// of executing one command against one device.
/// </summary>
/// <param name="DeviceId">The device targeted by the command.</param>
/// <param name="Result">The structured outcome of the command execution.</param>
/// <param name="OrderIndex">
/// The <see cref="SceneAction.OrderIndex"/> of the action that produced this command.
/// Used to preserve logical ordering after group expansion.
/// </param>
/// <remarks>
/// This structure maintains positional alignment with the resolved command list.
/// It allows consumers to:
/// <list type="bullet">
/// <item><description>associate results with specific devices</description></item>
/// <item><description>reconstruct the original scene action order</description></item>
/// <item><description>distinguish between multiple commands generated from one action</description></item>
/// </list>
/// </remarks>
public sealed record SceneExecutionEntry(
    Guid DeviceId,
    CommandResult Result,
    int OrderIndex);