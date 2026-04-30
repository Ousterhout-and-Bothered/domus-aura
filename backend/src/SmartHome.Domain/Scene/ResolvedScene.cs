namespace SmartHome.Domain.Scene;

/// <summary>
/// What you get back from <see cref="ISceneResolver.ResolveAsync"/>: a tree of commands
/// ready to execute, plus parallel lists that preserve device targeting and original action order.
/// </summary>
/// <param name="Composite">
/// The commands to run, in execution order. Executing this composite runs each child
/// command sequentially as part of the scene.
/// </param>
/// <param name="DeviceIdsInOrder">
/// One device ID per command in <paramref name="Composite"/>, in the same order.
/// After execution, each result can be paired with the device it acted on by index.
/// Failed-resolution slots (e.g., deleted devices or empty groups) use <see cref="Guid.Empty"/>.
/// </param>
/// <param name="OrderIndexesInOrder">
/// One scene action order index per command in <paramref name="Composite"/>, in the same order.
/// This preserves the original <see cref="SceneAction.OrderIndex"/> even after group expansion,
/// where a single action may produce multiple commands.
/// </param>
/// <remarks>
/// All three collections are parallel by design and always have the same length:
/// <list type="bullet">
/// <item><description><c>Composite.Children[i]</c> → the command executed</description></item>
/// <item><description><c>DeviceIdsInOrder[i]</c> → the device targeted</description></item>
/// <item><description><c>OrderIndexesInOrder[i]</c> → the originating scene action order</description></item>
/// </list>
/// This separation keeps commands device-agnostic while allowing the execution layer
/// to correctly associate results with both the target device and the originating scene action.
/// </remarks>
public sealed record ResolvedScene(
    CompositeCommand Composite,
    IReadOnlyList<Guid> DeviceIdsInOrder,
    IReadOnlyList<int> OrderIndexesInOrder);