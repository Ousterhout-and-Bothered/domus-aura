using SmartHome.Domain.Common.Exceptions;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Default implementation of <see cref="IDeviceCommandFactory"/>.
/// Resolves device commands through registered command builders.
/// </summary>
/// <param name="builders">The command builders available to create device commands.</param>
public sealed class DeviceCommandFactory(
    IEnumerable<IDeviceCommandBuilder> builders) : IDeviceCommandFactory
{
    private readonly Dictionary<string, List<IDeviceCommandBuilder>> _builders =
        builders
            .GroupBy(builder => builder.CommandName.ToLowerInvariant())
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

    /// <inheritdoc />
    public IDeviceCommand Create(string commandName, object? value, Device device)
    {
        var normalizedCommand = commandName.ToLowerInvariant();

        if (!_builders.TryGetValue(normalizedCommand, out var matchingBuilders))
        {
            throw new InvalidDomainArgumentException($"Unknown command: {commandName}");
        }

        var builder = matchingBuilders.FirstOrDefault(builder => builder.CanBuild(device));

        if (builder is null)
        {
            throw new InvalidDomainOperationException(
                $"Command '{commandName}' is not supported by device type '{device.Type}'.");
        }

        return builder.Build(value, device);
    }
}