using System.ComponentModel;
using SmartHome.Domain.Device;
using ModelContextProtocol.Server;

namespace SmartHome.Api.Services.Chat.Mcp.Tools;

/// <summary>
/// Provides MCP tools for turning powered devices on or off by location or across all matching devices.
/// </summary>
/// <param name="deviceService">The service used to retrieve devices and execute power commands.</param>
/// <param name="deviceType">The type of powered device targeted by this handler.</param>
/// <param name="deviceLabel">The readable device label used in tool names and responses.</param>
[McpServerToolType]
public sealed class PoweredDeviceTool(
    IDeviceService deviceService,
    DeviceType deviceType,
    string deviceLabel)
{
    /// <summary>
    /// Turns on powered devices.
    /// </summary>
    [McpServerTool(Name = "turn_on_devices")]
    [Description("Turn on devices of a specific type in a location, or use 'all'.")]
    public Task<string> TurnOnAsync(
        [Description("Room name like Living Room, or all.")]
        string location,
        CancellationToken cancellationToken = default)
    {
        return HandleAsync(location, true, cancellationToken);
    }

    /// <summary>
    /// Turns off powered devices.
    /// </summary>
    [McpServerTool(Name = "turn_off_devices")]
    [Description("Turn off devices of a specific type in a location, or use 'all'.")]
    public Task<string> TurnOffAsync(
        [Description("Room name like Living Room, or all.")]
        string location,
        CancellationToken cancellationToken = default)
    {
        return HandleAsync(location, false, cancellationToken);
    }

    /// <summary>
    /// Executes the powered device tool logic.
    /// </summary>
    private async Task<string> HandleAsync(
        string location,
        bool turnOn,
        CancellationToken cancellationToken = default)
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

        return BuildResponse(location, changed, alreadyCorrect, turnOn);
    }

    /// <summary>
    /// Builds a user-facing response summarizing the powered device operation.
    /// </summary>
    private string BuildResponse(string location, int changed, int alreadyCorrect, bool turnOn)
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