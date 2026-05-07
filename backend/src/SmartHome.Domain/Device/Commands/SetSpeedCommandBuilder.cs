using SmartHome.Domain.Device.Fan;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Builds speed commands for fan-controllable devices.
/// </summary>
public sealed class SetSpeedCommandBuilder : IDeviceCommandBuilder
{
    /// <inheritdoc />
    public string CommandName => "setspeed";

    /// <inheritdoc />
    public bool CanBuild(Device device) => device is IFanControllable;

    /// <inheritdoc />
    public IDeviceCommand Build(object? value, Device device)
    {
        return new SetSpeedCommand(
            (IFanControllable)device,
            CommandValueParser.ParseEnum<FanSpeed>(value),
            device);
    }
}