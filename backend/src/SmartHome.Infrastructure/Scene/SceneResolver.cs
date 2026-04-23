using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Device.Repository;
using SmartHome.Domain.Scene;
using SmartHome.Domain.Common.Exceptions;

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
            
            if (devices.Count == 0)
            {
                var failureMessage = action.TargetsDevice
                    ? $"Target device no longer registered (id: {action.DeviceId!.Value})."
                    : $"No devices match group target: {action.DeviceType} in {action.Location ?? "any location"}.";

                composite.Add(new FailedCommand(action.Operation, failureMessage));
                deviceIds.Add(Guid.Empty);
                continue;
            }
            
            foreach (var device in devices)
            {
                // Construction-time failures (e.g. Lock on a Light) must not abort the
                // whole resolution loop. CompositeCommand.Execute only catches exceptions
                // thrown from Execute(), not from construction — so we translate the
                // construction failure into a FailedCommand stub that reports itself as
                // a failed CommandResult when the composite runs. This preserves the
                // positional contract between Composite.Children and DeviceIdsInOrder.
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
                    command = new FailedCommand(action.Operation, ex.Message);
                }

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