using System.ComponentModel;
using SmartHome.Domain.Device;
using ModelContextProtocol.Server;

namespace SmartHome.Api.Services.Chat.Mcp.Tools;

/// <summary>
/// Provides MCP tools for turning thermostats on or off by location or across all thermostats.
/// </summary>
/// <param name="deviceService">The service used to retrieve thermostat devices and execute thermostat commands.</param>
[McpServerToolType]
public sealed class ThermostatPowerTool(
    IDeviceService deviceService)
{
    /// <summary>
    /// Turns on thermostats in a location or across all thermostats.
    /// </summary>
    /// <param name="location">Room name like Living Room, or all.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the thermostat power operation.</returns>
    [McpServerTool(Name = "turn_on_thermostats")]
    [Description("Turn on thermostats in a location, or use 'all' to turn on every thermostat.")]
    public Task<string> TurnOnThermostatsAsync(
        [Description("Room name like Living Room, or all.")]
        string location,
        CancellationToken cancellationToken = default)
    {
        return HandleAsync(location, true, cancellationToken);
    }

    /// <summary>
    /// Turns off thermostats in a location or across all thermostats.
    /// </summary>
    /// <param name="location">Room name like Living Room, or all.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the thermostat power operation.</returns>
    [McpServerTool(Name = "turn_off_thermostats")]
    [Description("Turn off thermostats in a location, or use 'all' to turn off every thermostat.")]
    public Task<string> TurnOffThermostatsAsync(
        [Description("Room name like Living Room, or all.")]
        string location,
        CancellationToken cancellationToken = default)
    {
        return HandleAsync(location, false, cancellationToken);
    }

    /// <summary>
    /// Executes the thermostat power tool using the supplied MCP arguments.
    /// </summary>
    /// <param name="location">The requested thermostat location, or all for every thermostat.</param>
    /// <param name="turnOn">A value indicating whether this operation turns thermostats on or off.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the thermostat power operation.</returns>
    private async Task<string> HandleAsync(
        string location,
        bool turnOn,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "I need a location to control thermostats.";
        }

        var thermostats = (await deviceService.GetAllDevicesAsync(
            ChatToolHelpers.ToLocationFilter(location),
            DeviceType.Thermostat,
            null,
            cancellationToken)).ToList();

        if (thermostats.Count == 0)
        {
            if (ChatToolHelpers.IsAll(location))
                return "No thermostats were found.";

            return $"No thermostats were found in {location}.";
        }

        var changed = 0;
        var alreadyCorrect = 0;

        foreach (var thermostat in thermostats)
        {
            var isAlreadyCorrect = turnOn
                ? thermostat.IsOn()
                : !thermostat.IsOn();

            if (isAlreadyCorrect)
            {
                alreadyCorrect++;
                continue;
            }

            await deviceService.ExecuteCommandAsync(
                thermostat.Id,
                "SetPower",
                turnOn ? "On" : "Off",
                cancellationToken);

            changed++;
        }

        return BuildResponse(location, changed, alreadyCorrect, turnOn);
    }

    /// <summary>
    /// Builds a user-facing response summarizing the thermostat power operation.
    /// </summary>
    /// <param name="location">The requested thermostat location, or all for every thermostat.</param>
    /// <param name="changed">The number of thermostats changed by the operation.</param>
    /// <param name="alreadyCorrect">The number of thermostats already in the requested power state.</param>
    /// <param name="turnOn">A value indicating whether this operation turns thermostats on or off.</param>
    /// <returns>A message summarizing the operation result.</returns>
    private static string BuildResponse(
        string location,
        int changed,
        int alreadyCorrect,
        bool turnOn)
    {
        var all = ChatToolHelpers.IsAll(location);
        var action = turnOn ? "Turned on" : "Turned off";
        var state = turnOn ? "on" : "off";

        if (!all)
        {
            return changed == 0
                ? $"The {location} thermostat was already {state}."
                : $"{action} the {location} thermostat.";
        }

        if (changed == 0)
        {
            return $"All thermostats were already {state}.";
        }

        if (alreadyCorrect == 0)
        {
            return $"{action} {ChatToolHelpers.Pluralize(changed, "thermostat")}.";
        }

        return $"{action} {ChatToolHelpers.Pluralize(changed, "thermostat")}. {ChatToolHelpers.SentenceCount(alreadyCorrect, "thermostat")} already {state}.";
    }
}