namespace SmartHome.Domain.Scene;

/// <summary>
/// What you get back from <see cref="ISceneResolver.ResolveAsync"/>: a tree of commands
/// ready to execute, plus a parallel list telling you which device each command targets.
/// </summary>
/// <param name="Composite">
/// The commands to run, in order. Execute this and each child runs as part of the batch.
/// </param>
/// <param name="DeviceIdsInOrder">
/// One device ID per command in <paramref name="Composite"/>, in the same order.
/// After execution, you can pair each result with the device it acted on by index.
/// Failed-resolution slots (deleted devices, empty groups) use <see cref="Guid.Empty"/>.
/// </param>
/// <remarks>
/// The two lists are parallel by design so the scene service can write history rows
/// against the right device without coupling <see cref="Device.Commands.CommandResult"/>
/// to device identity. Keeping commands and device IDs separate lets a command stay
/// device-agnostic while the surrounding execution context tracks the "who."
/// </remarks>
public sealed record ResolvedScene(
    CompositeCommand Composite,
    IReadOnlyList<Guid> DeviceIdsInOrder);