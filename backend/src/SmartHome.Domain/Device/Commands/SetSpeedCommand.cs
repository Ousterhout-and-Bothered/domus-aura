using SmartHome.Domain.Device.Fan;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Command to set the speed of a fan device.
/// </summary>
/// <param name="receiver">The fan device to operate on.</param>
/// <param name="speed">The target fan speed (Low, Medium, High).</param>
public sealed class SetSpeedCommand(IFanControllable receiver, FanSpeed speed) : IDeviceCommand
{
    public string OperationName => $"SetSpeed({speed})";
    
    /// <inheritdoc />
    public CommandResult Execute()
    {
        receiver.SetSpeed(speed);
        return new CommandResult(OperationName, true);
    }
}
