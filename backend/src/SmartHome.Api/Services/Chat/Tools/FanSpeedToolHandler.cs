using System.Text.Json;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Fan;

namespace SmartHome.Api.Services.Chat.Tools;

/// <summary>
/// Handles chat tool requests for setting fan speed by location or across all fans.
/// </summary>
/// <param name="deviceService">The service used to retrieve fan devices and execute fan commands.</param>
public sealed class FanSpeedToolHandler(
    IDeviceService deviceService) : IChatToolHandler
{
    /// <summary>
    /// Gets the tool name exposed to the language model.
    /// </summary>
    public string ToolName => "set_fan_speed";

    /// <summary>
    /// Gets the tool definition sent to the language model.
    /// </summary>
    public object ToolDefinition => new
    {
        type = "function",
        function = new
        {
            name = ToolName,
            description = "Set the speed for fans in a location, or use 'all' to set every fan.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    location = new
                    {
                        type = "string",
                        description = "Room name like Living Room, or all"
                    },
                    speed = new
                    {
                        type = "string",
                        description = "Fan speed: Low, Medium, or High"
                    }
                },
                required = new[] { "location", "speed" }
            }
        }
    };

    /// <summary>
    /// Executes the fan speed tool using the supplied model arguments.
    /// </summary>
    /// <param name="arguments">The tool arguments parsed from the model's tool call.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the fan speed operation.</returns>
    public async Task<string> HandleAsync(
        Dictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!ChatToolHelpers.TryGetString(arguments, "location", out var location))
        {
            return "I need a location to set fan speed.";
        }

        if (!ChatToolHelpers.TryGetString(arguments, "speed", out var speed))
        {
            return "Please provide a valid fan speed.";
        }

        var fans = (await deviceService.GetAllDevicesAsync(
            ChatToolHelpers.ToLocationFilter(location!),
            DeviceType.Fan,
            null,
            cancellationToken)).ToList();

        var result = await ChatToolHelpers.ExecuteOnPoweredDevicesAsync(
            fans,
            device => device is Fan fanDevice &&
                      string.Equals(fanDevice.Speed.ToString(), speed, StringComparison.OrdinalIgnoreCase),
            device => deviceService.ExecuteCommandAsync(
                device.Id,
                "SetSpeed",
                speed!,
                cancellationToken));

        return BuildResponse(
            location!,
            speed!,
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