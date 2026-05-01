using System.Text.Json;
using SmartHome.Domain.Device;

namespace SmartHome.Api.Services.Chat.Tools;

/// <summary>
/// Handles chat tool requests for setting light brightness by location or across all lights.
/// </summary>
/// <param name="deviceService">The service used to retrieve light devices and execute light commands.</param>
public sealed class LightBrightnessToolHandler(
    IDeviceService deviceService) : IChatToolHandler
{
    /// <summary>
    /// Gets the tool name exposed to the language model.
    /// </summary>
    public string ToolName => "set_light_brightness";

    /// <summary>
    /// Gets the tool definition sent to the language model.
    /// </summary>
    public object ToolDefinition => new
    {
        type = "function",
        function = new
        {
            name = ToolName,
            description = "Set brightness for lights in a location, or use 'all' to set every light.",
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
                    brightness = new
                    {
                        type = "integer",
                        description = "Brightness percentage from 10 to 100"
                    }
                },
                required = new[] { "location", "brightness" }
            }
        }
    };

    /// <summary>
    /// Executes the light brightness tool using the supplied model arguments.
    /// </summary>
    /// <param name="arguments">The tool arguments parsed from the model's tool call.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the light brightness operation.</returns>
    public async Task<string> HandleAsync(
        Dictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!ChatToolHelpers.TryGetString(arguments, "location", out var location) || location is null)
        {
            return "I need a location to set light brightness.";
        }

        if (!ChatToolHelpers.TryGetInt(arguments, "brightness", out var brightness))
        {
            return "I need a valid brightness value.";
        }

        var lights = (await deviceService.GetAllDevicesAsync(
            ChatToolHelpers.ToLocationFilter(location),
            DeviceType.Light,
            null,
            cancellationToken)).ToList();

        if (lights.Count == 0)
        {
            if (ChatToolHelpers.IsAll(location))
                return "No lights were found.";

            return $"No lights were found in {location}.";
        }

        var result = await ChatToolHelpers.ExecuteOnPoweredDevicesAsync(
            lights,
            device => device is SmartHome.Domain.Device.Light.Light lightDevice &&
                      lightDevice.Brightness == brightness,
            device => deviceService.ExecuteCommandAsync(
                device.Id,
                "SetBrightness",
                brightness.ToString(),
                cancellationToken));

        return BuildResponse(
            location,
            brightness,
            result.Changed,
            result.Unchanged,
            result.PoweredOff);
    }

    /// <summary>
    /// Builds a user-facing response summarizing the light brightness operation.
    /// </summary>
    /// <param name="location">The requested light location, or all for every light.</param>
    /// <param name="brightness">The requested brightness percentage.</param>
    /// <param name="changed">The number of lights changed by the operation.</param>
    /// <param name="unchanged">The number of lights already at the requested brightness.</param>
    /// <param name="poweredOff">The number of lights that could not be changed because they were powered off.</param>
    /// <returns>A message summarizing the operation result.</returns>
    private static string BuildResponse(
        string location,
        int brightness,
        int changed,
        int unchanged,
        int poweredOff)
    {

        var isAll = ChatToolHelpers.IsAll(location);
        var parts = new List<string>();

        if (changed > 0)
        {
            parts.Add(isAll
                ? $"Changed {ChatToolHelpers.Pluralize(changed, "light")} to {brightness}% brightness"
                : $"Set the {location} light brightness to {brightness}%");
        }

        if (unchanged > 0)
        {
            parts.Add(isAll
                ? $"{ChatToolHelpers.Pluralize(unchanged, "light")} already at {brightness}% brightness"
                : $"The light is already at {brightness}% brightness");
        }

        if (poweredOff > 0)
        {
            parts.Add($"{ChatToolHelpers.Pluralize(poweredOff, "light")} could not be changed because powered off");
        }

        return parts.Count > 0
            ? string.Join(". ", parts) + "."
            : "No matching lights were found.";
    }
}