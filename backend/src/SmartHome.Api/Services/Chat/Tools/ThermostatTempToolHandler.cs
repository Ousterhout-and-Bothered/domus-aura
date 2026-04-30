using System.Text.Json;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Thermostat;

namespace SmartHome.Api.Services.Chat.Tools;

/// <summary>
/// Handles chat tool requests for setting thermostat desired temperatures by location or across all thermostats.
/// </summary>
/// <param name="deviceService">The service used to retrieve thermostat devices and execute thermostat commands.</param>
public sealed class ThermostatTempToolHandler(
    IDeviceService deviceService) : IChatToolHandler
{
    public string ToolName => "set_thermostat_temperature";

    public object ToolDefinition => new
    {
        type = "function",
        function = new
        {
            name = ToolName,
            description = "Set the desired temperature of a thermostat in a location.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    location = new { type = "string", description = "Room name like Living Room, or all" },
                    temperature = new { type = "integer", description = "Desired temperature in °F" }
                },
                required = new[] { "location", "temperature" }
            }
        }
    };

    public async Task<string> HandleAsync(
        Dictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!ChatToolHelpers.TryGetInt(arguments, "temperature", out var temp))
        {
            return "Please provide a valid temperature.";
        }

        if (!ChatToolHelpers.TryGetString(arguments, "location", out var location))
        {
            return "I need a location to set thermostat temperature.";
        }

        var thermostats = (await deviceService.GetAllDevicesAsync(
            ChatToolHelpers.ToLocationFilter(location!),
            DeviceType.Thermostat,
            null,
            cancellationToken)).Cast<Thermostat>().ToList();

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

            await deviceService.ExecuteCommandAsync(
                thermostat.Id,
                "SetPower",
                "On",
                cancellationToken);

            await deviceService.ExecuteCommandAsync(
                thermostat.Id,
                "SetDesiredTemperature",
                temp.ToString(),
                cancellationToken);

            changed++;
        }

        return BuildResponse(location!, changed, alreadyCorrect, temp);
    }

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