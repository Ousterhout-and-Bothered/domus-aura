namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to toggle the power state of a device.
/// </summary>
/// <param name="receiver">The powerable device to operate on.</param>
/// <param name="targetState">The desired power state (On/Off).</param>
public sealed class SetPowerCommand(IPowerable receiver, PowerState targetState) : IDeviceCommand
{
    /// <inheritdoc />
    public void Execute()
    {
        if (targetState == PowerState.On)
            receiver.TurnOn();
        else
            receiver.TurnOff();
    }
}
