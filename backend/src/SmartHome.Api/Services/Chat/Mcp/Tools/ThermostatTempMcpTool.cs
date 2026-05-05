using System.ComponentModel;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Thermostat;
using ModelContextProtocol.Server;

namespace SmartHome.Api.Services.Chat.Mcp.Tools;

/// <summary>
/// Provides MCP tools for controlling thermostat desired temperatures.
/// </summary>
/// <param name="deviceService">The service used to retrieve thermostat devices and execute thermostat commands.</param>
[McpServerToolType]
public sealed class ThermostatTempTool(
    IDeviceService deviceService)
{
    /// <summary>
    /// Sets the desired temperature of thermostats by location or across all thermostats.
    /// </summary>
    /// <param name="location">Room name like Living Room, or all.</param>
    /// <param name="temperature">Desired temperature in °F.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the thermostat temperature operation.</returns>
    [McpServerTool(Name = "set_thermostat_temperature")]
    [Description("Set the desired temperature of a thermostat in a location.")]
    public async Task<string> SetThermostatTemperatureAsync(
        [Description("Room name like Living Room, or all.")]
        string location,
        [Description("Desired temperature in °F.")]
        int temperature,
        CancellationToken cancellationToken = default)
    {
        var temp = temperature;

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
            var isAlreadyAtDesired = thermostat.DesiredTemperature == temp;

            if (isAlreadyAtDesired)
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
                "SetDesiredTemperature",
                temp.ToString(),
                cancellationToken);

            changed++;
        }

        return BuildResponse(location, changed, alreadyCorrect, temp);
    }

    /// <summary>
    /// Builds a user-facing response summarizing the thermostat temperature operation.
    /// </summary>
    /// <param name="location">The requested thermostat location, or all for every thermostat.</param>
    /// <param name="changed">The number of thermostats changed by the operation.</param>
    /// <param name="alreadyCorrect">The number of thermostats already set to the requested temperature.</param>
    /// <param name="temp">The requested desired temperature.</param>
    /// <returns>A message summarizing the operation result.</returns>
    private static string BuildResponse(
        string location,
        int changed,
        int alreadyCorrect,
        int temp)
    {
        var isAll = ChatToolHelpers.IsAll(location);

        if (!isAll)
        {
            return changed == 0
                ? $"The {location} thermostat was already set to {temp}°F."
                : $"Set the {location} thermostat to {temp}°F.";
        }

        if (changed == 0)
        {
            return $"All {ChatToolHelpers.Pluralize(alreadyCorrect, "thermostat")} were already set to {temp}°F.";
        }

        if (alreadyCorrect == 0)
        {
            return $"Set {ChatToolHelpers.Pluralize(changed, "thermostat")} to {temp}°F.";
        }

        return $"Set {ChatToolHelpers.Pluralize(changed, "thermostat")} to {temp}°F. {ChatToolHelpers.SentenceCount(alreadyCorrect, "thermostat")} already set.";
    }
}