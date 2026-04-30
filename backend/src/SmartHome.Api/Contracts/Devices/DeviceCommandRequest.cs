namespace SmartHome.Api.Contracts.Devices;

/// <summary>
/// Represents a request to execute a command on a smart home device.
/// </summary>
/// <param name="Command">The name of the command to execute (e.g., "setPower", "setBrightness", "setSpeed", "setMode", "lock", "unlock", "setColor").</param>
/// <param name="Value">
/// The optional value associated with the command.
/// <list type="bullet">
///   <item><description><b>Power:</b> "on", "off"</description></item>
///   <item><description><b>Brightness:</b> Integer (10-100)</description></item>
///   <item><description><b>Fan Speed:</b> "Low", "Medium", "High"</description></item>
///   <item><description><b>Thermostat Mode:</b> "Heat", "Cool", "Auto"</description></item>
///   <item><description><b>Color:</b> Hex string (e.g., "#FF0000")</description></item>
///   <item><description><b>Lock/Unlock:</b> No value required</description></item>
/// </list>
/// <example>on</example>
/// </param>
public sealed record DeviceCommandRequest(
    string Command,
    object? Value = null);
