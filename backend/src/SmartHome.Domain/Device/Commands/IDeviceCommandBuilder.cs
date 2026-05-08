namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Defines a builder capable of creating a specific device command
/// for supported device types.
/// </summary>
public interface IDeviceCommandBuilder
{
    /// <summary>
    /// Gets the normalized command name supported by this builder.
    /// </summary>
    string CommandName { get; }

    /// <summary>
    /// Determines whether this builder can create a command
    /// for the specified device.
    /// </summary>
    /// <param name="device">The target device.</param>
    /// <returns>
    /// <c>true</c> if the device supports this command;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool CanBuild(Device device);

    /// <summary>
    /// Creates a device command for the specified device and value.
    /// </summary>
    /// <param name="value">The optional command value.</param>
    /// <param name="device">The target device.</param>
    /// <returns>The created device command.</returns>
    IDeviceCommand Build(object? value, Device device);
}