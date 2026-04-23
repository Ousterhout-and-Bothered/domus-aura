using SmartHome.Domain.Device;
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
    public async Task<ResolvedScene> ResolveAsync(
        DeviceScene scene,
        CancellationToken cancellationToken = default)
    {
        var composite = new CompositeCommand();
        var deviceIds = new List<Guid>();

        foreach (var action in scene.Actions)
        {
            var devices = await ResolveTargetsAsync(action, cancellationToken);

            foreach (var device in devices)
            {
                var command = commandFactory.Create(
                    commandName: action.Operation,
                    value: action.Value,
                    device: device);

                composite.Add(command);
                deviceIds.Add(device.Id);
            }
        }

        return new ResolvedScene(composite, deviceIds);
    }

    /// <summary>
    /// Expands a scene action's target into the concrete list of devices it applies to.
    /// A device-targeted action yields one device (or none if the device was deleted);
    /// a group-targeted action yields every device matching the type/location filter.
    /// </summary>
    private async Task<IReadOnlyList<Domain.Device.Device>> ResolveTargetsAsync(
        SceneAction action,
        CancellationToken cancellationToken)
    {
        if (action.TargetsDevice)
        {
            var device = await deviceRepository.GetByIdAsync(action.DeviceId!.Value, cancellationToken);
            return device is null ? [] : [device];
        }

        // Group target: query the repository for all matching devices.
        // Location may be null, which the repository interprets as "any location".
        return await deviceRepository.GetAllTrackedAsync(
            location: action.Location,
            type: action.DeviceType,
            cancellationToken: cancellationToken);
    }

}