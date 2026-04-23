namespace SmartHome.Domain.Scene;

/// <summary>
/// Resolves a <see cref="DeviceScene"/> definition into an executable
/// <see cref="CompositeCommand"/> populated with leaf commands bound to
/// currently-registered devices.
/// </summary>
/// <remarks>
/// Group targets are expanded at resolution time against the current device
/// registry, so devices added after scene creation are automatically included.
/// </remarks>
public interface ISceneResolver
{
    /// <summary>
    /// Builds a <see cref="CompositeCommand"/> from the given scene, expanding
    /// group targets and binding each action to a concrete device command.
    /// </summary>
    Task<ResolvedScene> ResolveAsync(DeviceScene scene, CancellationToken cancellationToken = default);
}