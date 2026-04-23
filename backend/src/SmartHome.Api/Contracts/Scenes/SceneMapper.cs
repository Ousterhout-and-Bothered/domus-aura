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
        // Targeting mode is inferred from which field is populated. The domain
        // constructor enforces "exactly one" via its XOR guard; this mapper just
        // dispatches to the correct factory.
        if (request.DeviceId.HasValue)
        {
            return SceneAction.ForDevice(
                deviceId: request.DeviceId.Value,
                operation: request.Operation,
                orderIndex: orderIndex,
                value: request.Value);
        }

        if (request.DeviceType.HasValue)
        {
            return SceneAction.ForGroup(
                deviceType: request.DeviceType.Value,
                location: request.Location,
                operation: request.Operation,
                orderIndex: orderIndex,
                value: request.Value);
        }

        // Neither targeting field was provided. Let the domain surface the error
        // message consistent with the XOR guard in SceneAction's private constructor.
        throw new InvalidDomainArgumentException(
            "Scene action must specify either deviceId or deviceType.");
    }
}