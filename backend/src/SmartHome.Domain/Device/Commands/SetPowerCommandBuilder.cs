using SmartHome.Domain.Common;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Builds power commands for devices that support power control.
/// </summary>
public sealed class SetPowerCommandBuilder : IDeviceCommandBuilder
{
    /// <inheritdoc />
    public string CommandName => "setpower";

    /// <inheritdoc />
    public bool CanBuild(Device device) => device is IPowerable;

    /// <inheritdoc />
    public IDeviceCommand Build(object? value, Device device)
    {
        return new SetPowerCommand(
            (IPowerable)device,
            CommandValueParser.ParseEnum<PowerState>(value),
            device);
    }
}