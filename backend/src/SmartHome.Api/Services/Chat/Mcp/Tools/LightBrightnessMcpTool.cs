using System.ComponentModel;
using SmartHome.Domain.Device;
using ModelContextProtocol.Server;

namespace SmartHome.Api.Services.Chat.Mcp.Tools;

/// <summary>
/// Handles chat tool requests for setting light brightness by location or across all lights.
/// </summary>
/// <param name="deviceService">The service used to retrieve light devices and execute light commands.</param>
[McpServerToolType]
public sealed class LightBrightnessTool(
    IDeviceService deviceService)
{
    /// <summary>
    /// Gets the tool name exposed to the language model.
    /// </summary>
    public const string ToolName = "set_light_brightness";

    /// <summary>
    /// Sets brightness for lights in a location, or use 'all' to set every light.
    /// </summary>
    /// <param name="location">Room name like Living Room, or all</param>
    /// <param name="brightness">Brightness percentage from 10 to 100</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the light brightness operation.</returns>
    [McpServerTool(Name = ToolName)]
    [Description("Set brightness for lights in a location, or use 'all' to set every light.")]
    public async Task<string> SetLightBrightnessAsync(
        [Description("Room name like Living Room, or all")]
        string location,
        [Description("Brightness percentage from 10 to 100")]
        int brightness,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "I need a location to set light brightness.";
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