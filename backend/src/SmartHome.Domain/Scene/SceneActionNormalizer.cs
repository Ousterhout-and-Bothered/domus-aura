namespace SmartHome.Domain.Scene;

/// <summary>
/// Normalizes scene actions into an executable order without mutating the persisted scene definition.
/// Ensures prerequisite operations, such as turning devices on and setting thermostats to Auto mode,
/// are present and ordered before dependent operations.
/// </summary>
public sealed class SceneActionNormalizer : ISceneActionNormalizer
{
    /// <inheritdoc />
    public DeviceScene Normalize(DeviceScene scene)
    {
        var orderedActions = scene.Actions
            .GroupBy(GetTargetKey)
            .SelectMany(NormalizeTargetGroup)
            .ToList();

        return new DeviceScene(scene.Id, scene.Name, orderedActions);
    }

    /// <summary>
    /// Normalizes all actions that target the same device or device group.
    /// </summary>
    /// <param name="group">The grouped scene actions that share the same target.</param>
    /// <returns>The normalized actions for the target group.</returns>
    private static IEnumerable<SceneAction> NormalizeTargetGroup(
        IGrouping<string, SceneAction> group)
    {
        var actions = group.ToList();

        var needsPowerOn = actions.Any(action =>
            action.Operation is
                "SetBrightness" or
                "SetColor" or
                "SetSpeed" or
                "SetMode" or
                "SetDesiredTemperature");

        var needsAutoMode = actions.Any(action =>
            action.Operation == "SetDesiredTemperature");

        if (needsPowerOn && actions.All(action => action.Operation != "TurnOn"))
        {
            actions.Add(CreatePrerequisiteAction(actions[0], "TurnOn", null));
        }

        if (needsAutoMode && actions.All(action =>
                action.Operation != "SetMode" ||
                !string.Equals(action.Value, "Auto", StringComparison.OrdinalIgnoreCase)))
        {
            actions.Add(CreatePrerequisiteAction(actions[0], "SetMode", "Auto"));
        }

        return actions
            .OrderBy(GetOperationPriority)
            .ThenBy(action => action.OrderIndex);
    }

    /// <summary>
    /// Builds a stable grouping key for actions that target the same device or device group.
    /// </summary>
    /// <param name="action">The scene action to group.</param>
    /// <returns>A string key representing the action target.</returns>
    private static string GetTargetKey(SceneAction action)
    {
        if (action.DeviceId is not null)
        {
            return action.DeviceId.Value.ToString();
        }

        return $"{action.DeviceType}:{action.Location}";
    }

    /// <summary>
    /// Gets the execution priority for a scene action.
    /// Lower values execute earlier than higher values.
    /// </summary>
    /// <param name="action">The scene action to prioritize.</param>
    /// <returns>The priority value used for ordering.</returns>
    private static int GetOperationPriority(SceneAction action)
    {
        return action.Operation switch
        {
            "TurnOn" => 0,
            "SetMode" when string.Equals(action.Value, "Auto", StringComparison.OrdinalIgnoreCase) => 1,
            "SetMode" => 2,
            "SetDesiredTemperature" => 3,
            "SetBrightness" => 4,
            "SetColor" => 5,
            "SetSpeed" => 6,
            "TurnOff" => 99,
            _ => 50
        };
    }

    /// <summary>
    /// Creates a prerequisite action using the same target as the source action.
    /// </summary>
    /// <param name="source">The source action whose target should be copied.</param>
    /// <param name="operation">The prerequisite operation to create.</param>
    /// <param name="value">The optional value for the prerequisite operation.</param>
    /// <returns>A new scene action targeting the same device or group as the source action.</returns>
    private static SceneAction CreatePrerequisiteAction(
        SceneAction source,
        string operation,
        string? value)
    {
        if (source.DeviceId.HasValue)
        {
            return SceneAction.ForDevice(
                source.DeviceId.Value,
                operation,
                source.OrderIndex,
                value);
        }

        return SceneAction.ForGroup(
            source.DeviceType!.Value,
            source.Location,
            operation,
            source.OrderIndex,
            value);
    }
}