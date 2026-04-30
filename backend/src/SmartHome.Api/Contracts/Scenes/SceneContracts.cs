using SmartHome.Domain.Device;

namespace SmartHome.Api.Contracts.Scenes;

/// <summary>
/// Request to create or update a scene. Each action targets either one specific
/// device (by <see cref="SceneActionRequest.DeviceId"/>) or a group of devices
/// (by <see cref="SceneActionRequest.DeviceType"/> and optional <see cref="SceneActionRequest.Location"/>).
/// Exactly one targeting mode must be used per action.
/// </summary>
/// <param name="Name">The user-facing name of the scene (e.g., "Goodnight").</param>
/// <param name="Actions">The ordered list of actions to execute when the scene runs.</param>
public sealed record SceneRequest(
    string Name,
    IReadOnlyList<SceneActionRequest> Actions);

/// <summary>
/// A single action within a scene: which device (or group of devices) to target,
/// which operation to perform, and the optional argument value.
/// </summary>
/// <param name="DeviceId">Set when targeting one specific device by id.</param>
/// <param name="DeviceType">Set when targeting a group of devices of this type.</param>
/// <param name="Location">Optional scope for a group target. Null matches any location.</param>
/// <param name="Operation">The command name (e.g., "SetBrightness", "Lock").</param>
/// <param name="Value">
/// The optional argument for the operation as a string. Null for parameterless
/// operations (Lock, Unlock); otherwise the value to apply (e.g., "50" for brightness,
/// "#FF0000" for color, "Heat" for thermostat mode).
/// </param>
public sealed record SceneActionRequest(
    Guid? DeviceId,
    DeviceType? DeviceType,
    string? Location,
    string Operation,
    string? Value);

/// <summary>
/// Response describing a saved scene.
/// </summary>
/// <param name="Id">The unique identifier of the scene.</param>
/// <param name="Name">The user-facing name of the scene.</param>
/// <param name="Actions">The ordered list of actions associated with the scene.</param>
public sealed record SceneResponse(
    Guid Id,
    string Name,
    IReadOnlyList<SceneActionResponse> Actions);

/// <summary>
/// Response describing a single action within a persisted scene.
/// </summary>
/// <param name="Id">The unique identifier of the action.</param>
/// <param name="DeviceId">The specific device targeted, if applicable.</param>
/// <param name="DeviceType">The device type targeted for group operations, if applicable.</param>
/// <param name="Location">The location filter for group operations, if applicable.</param>
/// <param name="Operation">The command name executed by this action.</param>
/// <param name="Value">The value associated with the operation, if any.</param>
/// <param name="OrderIndex">The execution order of the action within the scene.</param>
public sealed record SceneActionResponse(
    Guid Id,
    Guid? DeviceId,
    DeviceType? DeviceType,
    string? Location,
    string Operation,
    string? Value,
    int OrderIndex);

/// <summary>
/// Result of executing a scene: per-action outcomes and summary counts.
/// </summary>
/// <param name="SceneId">The identifier of the executed scene.</param>
/// <param name="SceneName">The name of the executed scene.</param>
/// <param name="Summary">Aggregate success and failure counts.</param>
/// <param name="Results">The ordered list of individual action results.</param>
public sealed record SceneExecutionResponse(
    Guid SceneId,
    string SceneName,
    SceneExecutionSummaryResponse Summary,
    IReadOnlyList<SceneExecutionResultResponse> Results);

/// <summary>
/// Summary counts for a scene execution.
/// </summary>
/// <param name="Succeeded">The number of actions that completed successfully.</param>
/// <param name="Failed">The number of actions that failed.</param>
public sealed record SceneExecutionSummaryResponse(
    int Succeeded,
    int Failed);

/// <summary>
/// A single action's outcome within a scene execution.
/// </summary>
/// <param name="OrderIndex">The execution order of the action.</param>
/// <param name="DeviceId">The identifier of the device the action was applied to.</param>
/// <param name="DeviceName">The user-facing name of the device.</param>
/// <param name="DeviceType">The type of device.</param>
/// <param name="Operation">The operation that was attempted.</param>
/// <param name="Value">The value used for the operation, if applicable.</param>
/// <param name="Status">The result status (e.g., "succeeded", "failed").</param>
/// <param name="Message">Optional message providing additional context or error details.</param>
public sealed record SceneExecutionResultResponse(
    int OrderIndex,
    Guid DeviceId,
    string DeviceName,
    DeviceType DeviceType,
    string Operation,
    object? Value,
    string Status,
    string? Message);