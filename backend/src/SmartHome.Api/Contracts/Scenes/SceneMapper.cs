using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Scene;
using SmartHome.Domain.Device.Commands;

namespace SmartHome.Api.Contracts.Scenes;

/// <summary>
/// Converts between scene domain entities and API contracts.
/// Responsible for shaping domain results into API-friendly responses,
/// including status interpretation and value normalization.
/// </summary>
internal static class SceneMapper
{
    /// <summary>
    /// Maps a <see cref="DeviceScene"/> domain entity to an API response.
    /// </summary>
    public static SceneResponse ToResponse(DeviceScene scene) =>
        new(
            Id: scene.Id,
            Name: scene.Name,
            Actions: scene.Actions
                .Select(ToResponse)
                .ToList());

    /// <summary>
    /// Maps a <see cref="SceneAction"/> to its API representation.
    /// </summary>
    public static SceneActionResponse ToResponse(SceneAction action) =>
        new(
            Id: action.Id,
            DeviceId: action.DeviceId,
            DeviceType: action.DeviceType,
            Location: action.Location,
            Operation: action.Operation,
            Value: action.Value,
            OrderIndex: action.OrderIndex);

    /// <summary>
    /// Maps a <see cref="SceneExecutionResult"/> to an API response.
    /// Converts domain-level command results into user-facing statuses,
    /// normalizes values for JSON output, preserves execution order, and
    /// surfaces the implicit-side-effect flags so the frontend can render
    /// "powered on automatically" / "switched to Auto mode" annotations.
    /// </summary>
    public static SceneExecutionResponse ToResponse(SceneExecutionResult result) =>
        new(
            SceneId: result.SceneId,
            SceneName: result.SceneName,
            Summary: new SceneExecutionSummaryResponse(
                Succeeded: result.SucceededCount,
                Failed: result.FailedCount),
            Results: result.Entries
                .Select(e => new SceneExecutionResultResponse(
                    OrderIndex: e.OrderIndex,
                    DeviceId: e.DeviceId,
                    DeviceName: e.Result.DeviceName,
                    DeviceType: e.Result.DeviceType,
                    Operation: e.Result.Operation,
                    Value: ParseValue(e.Result.Value),
                    Status: MapStatus(e.Result),
                    Message: SanitizeMessage(e.Result.Message),
                    ImplicitPowerOn: e.Result.ImplicitPowerOn,
                    ImplicitModeChange: e.Result.ImplicitModeChange))
                .ToList());

    /// <summary>
    /// Converts incoming API requests into domain <see cref="SceneAction"/> objects,
    /// assigning execution order based on request position.
    /// </summary>
    public static IEnumerable<SceneAction> ToDomain(IEnumerable<SceneActionRequest> requests) =>
        requests.Select((req, index) => ToDomain(req, index));

    /// <summary>
    /// Maps a single <see cref="SceneActionRequest"/> to a domain <see cref="SceneAction"/>.
    /// Enforces that exactly one targeting mode is specified (device or group).
    /// </summary>
    private static SceneAction ToDomain(SceneActionRequest request, int orderIndex)
    {
        var hasDeviceId = request.DeviceId.HasValue;
        var hasDeviceType = request.DeviceType.HasValue;

        if (hasDeviceId && hasDeviceType)
        {
            throw new InvalidDomainArgumentException(
                "Scene action must specify either deviceId or deviceType, not both.");
        }

        if (!hasDeviceId && !hasDeviceType)
        {
            throw new InvalidDomainArgumentException(
                "Scene action must specify either deviceId or deviceType.");
        }

        if (hasDeviceId)
        {
            return SceneAction.ForDevice(
                deviceId: request.DeviceId!.Value,
                operation: request.Operation,
                orderIndex: orderIndex,
                value: request.Value);
        }

        return SceneAction.ForGroup(
            deviceType: request.DeviceType!.Value,
            location: request.Location,
            operation: request.Operation,
            orderIndex: orderIndex,
            value: request.Value);
    }

    /// <summary>
    /// Maps a <see cref="CommandResult"/> to a user-facing status string.
    /// Distinguishes between successful state changes, no-op operations,
    /// and failures.
    /// </summary>
    private static string MapStatus(CommandResult result)
    {
        if (!result.Success)
        {
            return "failed";
        }

        if (!result.IsNoOp)
        {
            return "changed";
        }

        return result.Operation switch
        {
            "SetPower" when result.Value == "On" => "already_on",
            "SetPower" when result.Value == "Off" => "already_off",
            "Lock" => "already_locked",
            "Unlock" => "already_unlocked",
            _ => "already_in_requested_state"
        };
    }

    /// <summary>
    /// Attempts to normalize string values for API output.
    /// Currently converts numeric strings to integers; otherwise returns the original string.
    /// </summary>
    private static object? ParseValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, out var intValue))
        {
            return intValue;
        }

        return value;
    }

    /// <summary>
    /// Translates domain-level messages into user-friendly API output.
    /// Known domain messages may be replaced with standardized responses.
    /// </summary>
    private static string? SanitizeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        if (message.Contains("Invalid transition", StringComparison.OrdinalIgnoreCase))
        {
            return "Device is already in the requested state.";
        }

        return message;
    }
}