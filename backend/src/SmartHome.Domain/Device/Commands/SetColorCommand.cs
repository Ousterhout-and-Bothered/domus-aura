using SmartHome.Domain.Device.Light;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the color of a colorable device.
/// </summary>
/// <param name="receiver">The color-controllable light to operate on.</param>
/// <param name="colorHex">The target color in hex format.</param>
public sealed class SetColorCommand(IColorable receiver, string colorHex) : IDeviceCommand
{
    /// <inheritdoc />
    public void Execute()
    {
        receiver.SetColor(colorHex);
    }
}
