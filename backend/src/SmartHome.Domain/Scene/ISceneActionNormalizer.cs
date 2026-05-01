namespace SmartHome.Domain.Scene;

/// <summary>
/// Defines a service that prepares scene actions for execution without mutating
/// the persisted scene definition or weakening device domain rules.
/// </summary>
public interface ISceneActionNormalizer
{
    /// <summary>
    /// Returns an executable copy of the scene with prerequisite operations inserted
    /// and ordered before dependent operations.
    /// </summary>
    /// <param name="scene">The scene definition to normalize.</param>
    /// <returns>
    /// A scene copy whose actions are safe to resolve and execute against device state machines.
    /// </returns>
    DeviceScene Normalize(DeviceScene scene);
}