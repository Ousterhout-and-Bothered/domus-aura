using System.ComponentModel;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Thermostat;
using ModelContextProtocol.Server;

namespace SmartHome.Api.Services.Chat.Mcp.Tools;

/// <summary>
/// Handles chat tool requests for setting thermostat mode by location or across all thermostats.
/// </summary>
/// <param name="deviceService">The service used to retrieve thermostat devices and execute commands.</param>
[McpServerToolType]
public sealed class ThermostatModeTool(
    IDeviceService deviceService)
{
    /// <summary>
    /// Gets the tool name exposed to the language model.
    /// </summary>
    public const string ToolName = "set_thermostat_mode";

    /// <summary>
    /// Sets the operating mode for thermostats in a location, or use 'all' to set every thermostat.
    /// </summary>
    /// <param name="location">Room name like Living Room, or all</param>
    /// <param name="mode">Thermostat mode: Heat, Cool, or Auto</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the thermostat mode operation.</returns>
    [McpServerTool(Name = ToolName)]
    [Description("Set the operating mode for thermostats in a location, or use 'all' to set every thermostat.")]
    public async Task<string> SetThermostatModeAsync(
        [Description("Room name like Living Room, or all")]
        string location,
        [Description("Thermostat mode: Heat, Cool, or Auto")]
        string mode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "I need a location to set thermostat mode.";
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            return "Please provide a valid thermostat mode.";
        }

        if (!Enum.TryParse<ThermostatMode>(mode, true, out var parsedMode))
        {
            return "Please provide a valid mode: Heat, Cool, or Auto.";
        }

        var normalizedMode = parsedMode.ToString();

        var thermostats = (await deviceService.GetAllDevicesAsync(
            ChatToolHelpers.ToLocationFilter(location),
            DeviceType.Thermostat,
            null,
            cancellationToken)).Cast<Thermostat>().ToList();

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
            var isAlreadyCorrect = thermostat.Mode == parsedMode;

            if (isAlreadyCorrect)
            {
                alreadyCorrect++;
                continue;
            }

            if (!thermostat.IsOn())
            {
                await deviceService.ExecuteCommandAsync(
                    thermostat.Id,
                    "SetPower",
                    "On",
                    cancellationToken);
            }

            await deviceService.ExecuteCommandAsync(
                thermostat.Id,
                "SetMode",
                normalizedMode,
                cancellationToken);

            changed++;
        }

        return BuildResponse(location, normalizedMode, changed, alreadyCorrect);
    }

    /// <summary>
    /// Builds a user-facing response summarizing the thermostat mode operation.
    /// </summary>
    private static string BuildResponse(
        string location,
        string mode,
        int changed,
        int alreadyCorrect)
    {
        var isAll = ChatToolHelpers.IsAll(location);

        if (!isAll)
        {
            return changed == 0
                ? $"The {location} thermostat was already in {mode} mode."
                : $"Set the {location} thermostat to {mode} mode.";
        }

        if (changed == 0)
        {
            return $"All {ChatToolHelpers.Pluralize(alreadyCorrect, "thermostat")} were already in {mode} mode.";
        }

        if (alreadyCorrect == 0)
        {
            return $"Set {ChatToolHelpers.Pluralize(changed, "thermostat")} to {mode} mode.";
        }

        return $"Set {ChatToolHelpers.Pluralize(changed, "thermostat")} to {mode} mode. {ChatToolHelpers.SentenceCount(alreadyCorrect, "thermostat")} already set.";
    }
}