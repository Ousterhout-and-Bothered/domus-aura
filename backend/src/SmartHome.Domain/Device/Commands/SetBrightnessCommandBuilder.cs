using SmartHome.Domain.Device.Light;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Builds brightness commands for dimmable devices.
/// </summary>
public sealed class SetBrightnessCommandBuilder : IDeviceCommandBuilder
{
    /// <inheritdoc />
    public string CommandName => "setbrightness";

    /// <inheritdoc />
    public bool CanBuild(Device device) => device is IDimmable;

    /// <inheritdoc />
    public IDeviceCommand Build(object? value, Device device)
    {
        return new SetBrightnessCommand(
            (IDimmable)device,
            (IPowerable)device,
            CommandValueParser.ParseInt(value),
            device);
    }
}