using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Scene;

namespace SmartHome.Api.Contracts.Scenes;

/// <summary>
/// Converts between scene domain entities and API contracts.
/// </summary>
internal static class SceneMapper
{
    public static SceneResponse ToResponse(DeviceScene scene) =>
        new(
            Id: scene.Id,
            Name: scene.Name,
            Actions: scene.Actions
                .Select(ToResponse)
                .ToList());

    public static SceneActionResponse ToResponse(SceneAction action) =>
        new(
            Id: action.Id,
            DeviceId: action.DeviceId,
            DeviceType: action.DeviceType,
            Location: action.Location,
            Operation: action.Operation,
            Value: action.Value,
            OrderIndex: action.OrderIndex);

    public static SceneExecutionResponse ToResponse(SceneExecutionResult result) =>
        new(
            SceneId: result.SceneId,
            SceneName: result.SceneName,
            SucceededCount: result.SucceededCount,
            FailedCount: result.FailedCount,
            Entries: result.Entries
                .Select(e => new SceneExecutionEntryResponse(
                    DeviceId: e.DeviceId,
                    Operation: e.Result.Operation,
                    Success: e.Result.Success,
                    Message: e.Result.Message))
                .ToList());

    public static IEnumerable<SceneAction> ToDomain(IEnumerable<SceneActionRequest> requests) =>
        requests.Select((req, index) => ToDomain(req, index));

    private static SceneAction ToDomain(SceneActionRequest request, int orderIndex)
    {
        // Enforce exactly-one targeting at the request boundary. The domain's
        // XOR guard runs inside SceneAction's private constructor, but by then
        // each factory has already accepted only its own field — so the mapper
        // would silently prefer one and discard the other rather than surface
        // the conflict. Validate here so the caller gets an honest 400 about
        // an over- or under-specified request.
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
}