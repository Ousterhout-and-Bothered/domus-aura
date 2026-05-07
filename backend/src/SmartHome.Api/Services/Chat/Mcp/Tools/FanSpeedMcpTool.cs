using System.ComponentModel;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Fan;
using ModelContextProtocol.Server;

namespace SmartHome.Api.Services.Chat.Mcp.Tools;

/// <summary>
/// Handles chat tool requests for setting fan speed by location or across all fans.
/// </summary>
/// <param name="deviceService">The service used to retrieve fan devices and execute fan commands.</param>
[McpServerToolType]
public sealed class FanSpeedTool(
    IDeviceService deviceService)
{
    /// <summary>
    /// Gets the tool name exposed to the language model.
    /// </summary>
    public const string ToolName = "set_fan_speed";

    /// <summary>
    /// Sets the speed for fans in a location, or use 'all' to set every fan.
    /// </summary>
    /// <param name="location">Room name like Living Room, or all</param>
    /// <param name="speed">Fan speed: Low, Medium, or High</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the fan speed operation.</returns>
    [McpServerTool(Name = ToolName)]
    [Description("Set the speed for fans in a location, or use 'all' to set every fan.")]
    public async Task<string> SetFanSpeedAsync(
        [Description("Room name like Living Room, or all")]
        string location,
        [Description("Fan speed: Low, Medium, or High")]
        string speed,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "I need a location to set fan speed.";
        }

        if (string.IsNullOrWhiteSpace(speed))
        {
            return "Please provide a valid fan speed.";
        }

        if (!Enum.TryParse<FanSpeed>(speed, true, out var parsedSpeed))
        {
            return "Please provide a valid fan speed: Low, Medium, or High.";
        }

        var normalizedSpeed = parsedSpeed.ToString();

        var fans = (await deviceService.GetAllDevicesAsync(
            ChatToolHelpers.ToLocationFilter(location),
            DeviceType.Fan,
            null,
            cancellationToken)).ToList();

        if (fans.Count == 0)
        {
            if (ChatToolHelpers.IsAll(location))
                return "No fans were found.";

            return $"No fans were found in {location}.";
        }

        var result = await ChatToolHelpers.ExecuteOnPoweredDevicesAsync(
            fans,
            device => device is Fan fanDevice &&
                      fanDevice.Speed == parsedSpeed,
            device => deviceService.ExecuteCommandAsync(
                device.Id,
                "SetSpeed",
                normalizedSpeed,
                cancellationToken));

        return BuildResponse(
            location,
            normalizedSpeed,
            result.Changed,
            result.Unchanged,
            result.PoweredOff);
    }

    /// <summary>
    /// Builds a user-facing response summarizing the fan speed operation.
    /// </summary>
    /// <param name="location">The requested fan location, or all for every fan.</param>
    /// <param name="speed">The requested fan speed.</param>
    /// <param name="changed">The number of fans changed by the operation.</param>
    /// <param name="unchanged">The number of fans already at the requested speed.</param>
    /// <param name="poweredOff">The number of fans that could not be changed because they were powered off.</param>
    /// <returns>A message summarizing the operation result.</returns>
    private static string BuildResponse(
        string location,
        string speed,
        int changed,
        int unchanged,
        int poweredOff)
    {
        var isAll = ChatToolHelpers.IsAll(location);
        var parts = new List<string>();

        if (changed > 0)
        {
            parts.Add(isAll
                ? $"Changed {ChatToolHelpers.Pluralize(changed, "fan")} to {speed} speed"
                : $"Set the {location} fan to {speed} speed");
        }

        if (unchanged > 0)
        {
            parts.Add(isAll
                ? $"{ChatToolHelpers.Pluralize(unchanged, "fan")} already at {speed} speed"
                : $"The fan is already at {speed.ToLower()} speed");
        }

        if (poweredOff > 0)
        {
            parts.Add($"{ChatToolHelpers.Pluralize(poweredOff, "fan")} could not be changed because powered off");
        }

        return parts.Count > 0
            ? string.Join(". ", parts) + "."
            : "No matching fans were found.";
    }
}