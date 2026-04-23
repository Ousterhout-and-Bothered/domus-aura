namespace SmartHome.Domain.Scene;

/// <summary>
/// The output of <see cref="ISceneResolver.ResolveAsync"/>: a command tree ready
/// to execute, paired with the device IDs each leaf command targets.
/// </summary>
/// <param name="Composite">
/// The command tree to execute. Children are in execution order.
/// </param>
/// <param name="DeviceIdsInOrder">
/// The device ID each child of <paramref name="Composite"/> targets, in the same
/// order. <c>DeviceIdsInOrder[i]</c> corresponds to <c>Composite.Children[i]</c>,
/// and (after execution) to the i-th <see cref="Device.Commands.CommandResult"/>.
/// </param>
/// <remarks>
/// This pairing lets the scene service correlate each command's result with the
/// device it affected when writing to the command history, without putting device
/// identity on <see cref="Device.Commands.CommandResult"/> itself.
/// </remarks>
public sealed record ResolvedScene(
    CompositeCommand Composite,
    IReadOnlyList<Guid> DeviceIdsInOrder);