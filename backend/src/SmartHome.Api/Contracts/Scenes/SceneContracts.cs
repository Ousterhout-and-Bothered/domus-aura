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
public sealed record SceneResponse(
    Guid Id,
    string Name,
    IReadOnlyList<SceneActionResponse> Actions);

/// <summary>
/// Response describing a single action within a persisted scene.
/// </summary>
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
public sealed record SceneExecutionResponse(
    Guid SceneId,
    string SceneName,
    int SucceededCount,
    int FailedCount,
    IReadOnlyList<SceneExecutionEntryResponse> Entries);

/// <summary>
/// A single action's outcome within a scene execution.
/// </summary>
public sealed record SceneExecutionEntryResponse(
    Guid DeviceId,
    string DeviceName,
    DeviceType DeviceType,
    string Operation,
    string? Value,
    bool Success,
    string? Message);