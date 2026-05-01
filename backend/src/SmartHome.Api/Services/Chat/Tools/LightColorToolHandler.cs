using System.Text.Json;
using SmartHome.Domain.Device;

namespace SmartHome.Api.Services.Chat.Tools;

/// <summary>
/// Handles chat tool requests for setting light color by location or across all lights.
/// </summary>
/// <param name="deviceService">The service used to retrieve light devices and execute light commands.</param>
public sealed class LightColorToolHandler(
    IDeviceService deviceService) : IChatToolHandler
{
    /// <summary>
    /// Gets the tool name exposed to the language model.
    /// </summary>
    public string ToolName => "set_light_color";

    /// <summary>
    /// Gets the tool definition sent to the language model.
    /// </summary>
    public object ToolDefinition => new
    {
        type = "function",
        function = new
        {
            name = ToolName,
            description = "Set the color for lights in a location, or use 'all' to set every light.",
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
                    color = new
                    {
                        type = "string",
                        description = "Light color as a hex value like #FF0000, #00FF00, or #FFFFFF"
                    }
                },
                required = new[] { "location", "color" }
            }
        }
    };

    /// <summary>
    /// Executes the light color tool using the supplied model arguments.
    /// </summary>
    /// <param name="arguments">The tool arguments parsed from the model's tool call.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the light color operation.</returns>
    public async Task<string> HandleAsync(
        Dictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!ChatToolHelpers.TryGetString(arguments, "location", out var location) || location is null)
        {
            return "I need a location to set light color.";
        }

        if (!ChatToolHelpers.TryGetString(arguments, "color", out var color) || color is null)
        {
            return "Please provide a valid color.";
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
                      string.Equals(lightDevice.ColorHex, color, StringComparison.OrdinalIgnoreCase),
            device => deviceService.ExecuteCommandAsync(
                device.Id,
                "SetColor",
                color,
                cancellationToken));

        return BuildResponse(
            location,
            color,
            result.Changed,
            result.Unchanged,
            result.PoweredOff);
    }

    /// <summary>
    /// Converts known hex color values into readable color names.
    /// </summary>
    /// <param name="hex">The hex color value to convert.</param>
    /// <returns>The matching color name when known; otherwise, the original hex value.</returns>
    private static string ToColorName(string hex)
    {
        return hex.ToUpper() switch
        {
            "#FF0000" => "red",
            "#00FF00" => "green",
            "#0000FF" => "blue",
            "#FFFF00" => "yellow",
            "#FFFFFF" => "white",
            "#FFA500" => "orange",
            "#800080" => "purple",
            "#FFC0CB" => "pink",
            "#000000" => "black",
            _ => hex
        };
    }

    /// <summary>
    /// Builds a user-facing response summarizing the light color operation.
    /// </summary>
    /// <param name="location">The requested light location, or all for every light.</param>
    /// <param name="color">The requested light color.</param>
    /// <param name="changed">The number of lights changed by the operation.</param>
    /// <param name="unchanged">The number of lights already set to the requested color.</param>
    /// <param name="poweredOff">The number of lights that could not be changed because they were powered off.</param>
    /// <returns>A message summarizing the operation result.</returns>
    private static string BuildResponse(
        string location,
        string color,
        int changed,
        int unchanged,
        int poweredOff)
    {
        var isAll = ChatToolHelpers.IsAll(location);
        var colorName = ToColorName(color);
        var parts = new List<string>();

        if (changed > 0)
        {
            parts.Add(isAll
                ? $"Changed {ChatToolHelpers.Pluralize(changed, "light")} to {colorName}"
                : $"Set the {location} light color to {colorName}");
        }

        if (unchanged > 0)
        {
            parts.Add(isAll
                ? $"{ChatToolHelpers.Pluralize(unchanged, "light")} already {colorName}"
                : $"The light is already {colorName}");
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