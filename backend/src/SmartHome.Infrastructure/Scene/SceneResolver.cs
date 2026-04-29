using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Scene;

namespace SmartHome.Infrastructure.Scene;

/// <summary>
/// Default implementation of <see cref="ISceneResolver"/>. Expands group targets
/// via <see cref="IDeviceRepository"/> and uses <see cref="IDeviceCommandFactory"/>
/// to produce leaf commands.
/// </summary>
public sealed class SceneResolver(
    IDeviceRepository deviceRepository,
    IDeviceCommandFactory commandFactory) : ISceneResolver
{
    /// <inheritdoc />
    public async Task<ResolvedScene> ResolveAsync(
        DeviceScene scene,
        CancellationToken cancellationToken = default)
    {
        var composite = new CompositeCommand();
        var deviceIds = new List<Guid>();
        var orderIndexes = new List<int>();

        foreach (var action in scene.Actions)
        {
            var devices = await ResolveTargetsAsync(action, cancellationToken);

            if (devices.Count == 0)
            {
                var failureMessage = action.TargetsDevice
                    ? $"Target device no longer registered (id: {action.DeviceId!.Value})."
                    : $"No devices match group target: {action.DeviceType} in {action.Location ?? "any location"}.";

                composite.Add(new FailedCommand(
                    operationName: action.Operation,
                    message: failureMessage,
                    deviceId: action.DeviceId,
                    deviceType: action.DeviceType,
                    value: action.Value));

                deviceIds.Add(Guid.Empty);
                orderIndexes.Add(action.OrderIndex);
                continue;
            }

            foreach (var device in devices)
            {
                IDeviceCommand command;

                try
                {
                    command = commandFactory.Create(
                        commandName: action.Operation,
                        value: action.Value,
                        device: device);
                }
                catch (DomainException ex)
                {
                    command = new FailedCommand(
                        operationName: action.Operation,
                        message: ex.Message,
                        deviceId: device.Id,
                        deviceName: device.Name,
                        deviceType: device.Type,
                        value: action.Value);
                }

                composite.Add(command);
                deviceIds.Add(device.Id);
                orderIndexes.Add(action.OrderIndex);
            }
        }

        return new ResolvedScene(composite, deviceIds, orderIndexes);
    }

    /// <summary>
    /// Expands a scene action's target into the concrete list of devices it applies to.
    /// A device-targeted action yields one device, or none if the device was deleted;
    /// a group-targeted action yields every device matching the type/location filter.
    /// </summary>
    /// <param name="action">The scene action whose target should be resolved.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>The devices targeted by the scene action.</returns>
    private async Task<IReadOnlyList<Domain.Device.Device>> ResolveTargetsAsync(
        SceneAction action,
        CancellationToken cancellationToken)
    {
        if (action.TargetsDevice)
        {
            var device = await deviceRepository.GetByIdAsync(action.DeviceId!.Value, cancellationToken);
            return device is null ? [] : [device];
        }

        return await deviceRepository.GetAllTrackedAsync(
            location: action.Location,
            type: action.DeviceType,
            cancellationToken: cancellationToken);
    }
}