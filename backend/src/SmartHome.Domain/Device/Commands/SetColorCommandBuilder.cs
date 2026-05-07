using SmartHome.Domain.Device.Light;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Builds color commands for color-capable devices.
/// </summary>
public sealed class SetColorCommandBuilder : IDeviceCommandBuilder
{
    /// <inheritdoc />
    public string CommandName => "setcolor";

    /// <inheritdoc />
    public bool CanBuild(Device device) => device is IColorable;

    /// <inheritdoc />
    public IDeviceCommand Build(object? value, Device device)
    {
        return new SetColorCommand(
            (IColorable)device,
            (IPowerable)device,
            value?.ToString() ?? string.Empty,
            device);
    }
}