using SmartHome.Domain.Device.Light;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the brightness of a dimmable device.
/// </summary>
/// <param name="receiver">The dimmable device to operate on.</param>
/// <param name="brightness">The target brightness level ( 0-100).</param>
public sealed class SetBrightnessCommand(IDimmable receiver, int brightness) : IDeviceCommand
{
    /// <inheritdoc />
    public void Execute()
    {
        receiver.SetBrightness(brightness);
    }
}
