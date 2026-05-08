using System.ComponentModel;
using SmartHome.Domain.Device;
using ModelContextProtocol.Server;

namespace SmartHome.Api.Services.Chat.Mcp.Tools;

/// <summary>
/// Provides MCP tools for turning powered devices (lights, fans) on or off by location.
/// </summary>
/// <param name="deviceService">The service used to retrieve devices and execute power commands.</param>
[McpServerToolType]
public sealed class PoweredDeviceTool(IDeviceService deviceService)
{
    [McpServerTool(Name = "turn_on_lights")]
    [Description("Turn on lights in a location, or use 'all' to turn on lights everywhere.")]
    public Task<string> TurnOnLightsAsync(
        [Description("Room name like Living Room, or 'all'.")]
        string location,
        CancellationToken cancellationToken = default)
        => HandleAsync(DeviceType.Light, "light", location, true, cancellationToken);

    [McpServerTool(Name = "turn_off_lights")]
    [Description("Turn off lights in a location, or use 'all' to turn off lights everywhere.")]
    public Task<string> TurnOffLightsAsync(
        [Description("Room name like Living Room, or 'all'.")]
        string location,
        CancellationToken cancellationToken = default)
        => HandleAsync(DeviceType.Light, "light", location, false, cancellationToken);

    [McpServerTool(Name = "turn_on_fans")]
    [Description("Turn on fans in a location, or use 'all' to turn on fans everywhere.")]
    public Task<string> TurnOnFansAsync(
        [Description("Room name like Living Room, or 'all'.")]
        string location,
        CancellationToken cancellationToken = default)
        => HandleAsync(DeviceType.Fan, "fan", location, true, cancellationToken);

    [McpServerTool(Name = "turn_off_fans")]
    [Description("Turn off fans in a location, or use 'all' to turn off fans everywhere.")]
    public Task<string> TurnOffFansAsync(
        [Description("Room name like Living Room, or 'all'.")]
        string location,
        CancellationToken cancellationToken = default)
        => HandleAsync(DeviceType.Fan, "fan", location, false, cancellationToken);

    private async Task<string> HandleAsync(
        DeviceType deviceType,
        string deviceLabel,
        string location,
        bool turnOn,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return $"I need a location to control {deviceLabel}s.";
        }

        var targets = (await deviceService.GetAllDevicesAsync(
            ChatToolHelpers.ToLocationFilter(location),
            deviceType,
            null,
            cancellationToken)).ToList();

        if (targets.Count == 0)
        {
            if (ChatToolHelpers.IsAll(location))
                return $"No {ChatToolHelpers.Pluralize(0, deviceLabel)} were found.";

            return $"No {ChatToolHelpers.Pluralize(0, deviceLabel)} were found in {location}.";
        }

        var changed = 0;
        var alreadyCorrect = 0;

        foreach (var device in targets)
        {
            var isAlreadyCorrect = turnOn ? device.IsOn() : !device.IsOn();

            if (isAlreadyCorrect)
            {
                alreadyCorrect++;
                continue;
            }

            await deviceService.ExecuteCommandAsync(
                device.Id,
                "SetPower",
                turnOn ? "On" : "Off",
                cancellationToken);

            changed++;
        }

        return BuildResponse(deviceLabel, location, changed, alreadyCorrect, turnOn);
    }

    private static string BuildResponse(
        string deviceLabel,
        string location,
        int changed,
        int alreadyCorrect,
        bool turnOn)
    {
        var all = ChatToolHelpers.IsAll(location);
        var action = turnOn ? "Turned on" : "Turned off";
        var state = turnOn ? "on" : "off";

        if (changed == 0)
        {
            return all
                ? $"All {ChatToolHelpers.Pluralize(alreadyCorrect, deviceLabel)} were already {state}."
                : $"The {deviceLabel} in {location} was already {state}.";
        }

        if (alreadyCorrect == 0)
        {
            return all
                ? $"{action} {ChatToolHelpers.Pluralize(changed, deviceLabel)}."
                : $"{action} {ChatToolHelpers.Pluralize(changed, deviceLabel)} in {location}.";
        }

        return all
            ? $"{action} {ChatToolHelpers.Pluralize(changed, deviceLabel)}. {ChatToolHelpers.SentenceCount(alreadyCorrect, deviceLabel)} already {state}."
            : $"{action} {ChatToolHelpers.Pluralize(changed, deviceLabel)} in {location}. {ChatToolHelpers.SentenceCount(alreadyCorrect, deviceLabel)} already {state}.";
    }
}