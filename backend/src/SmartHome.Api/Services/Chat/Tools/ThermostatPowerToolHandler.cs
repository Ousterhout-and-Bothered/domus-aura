using System.Text.Json;
using SmartHome.Domain.Device;

namespace SmartHome.Api.Services.Chat.Tools;

/// <summary>
/// Handles chat tool requests for turning thermostats on or off by location or across all thermostats.
/// </summary>
/// <param name="deviceService">The service used to retrieve thermostat devices and execute thermostat commands.</param>
/// <param name="turnOn">A value indicating whether this handler turns thermostats on or off.</param>
public sealed class ThermostatPowerToolHandler(
    IDeviceService deviceService,
    bool turnOn) : IChatToolHandler
{
    /// <summary>
    /// Gets the tool name exposed to the language model.
    /// </summary>
    public string ToolName => turnOn ? "turn_on_thermostats" : "turn_off_thermostats";

    /// <summary>
    /// Gets the tool definition sent to the language model.
    /// </summary>
    public object ToolDefinition => new
    {
        type = "function",
        function = new
        {
            name = ToolName,
            description = turnOn
                ? "Turn on thermostats in a location, or use 'all' to turn on every thermostat."
                : "Turn off thermostats in a location, or use 'all' to turn off every thermostat.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    location = new
                    {
                        type = "string",
                        description = "Room name like Living Room, or all"
                    }
                },
                required = new[] { "location" }
            }
        }
    };

    /// <summary>
    /// Executes the thermostat power tool using the supplied model arguments.
    /// </summary>
    /// <param name="arguments">The tool arguments parsed from the model's tool call.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the thermostat power operation.</returns>
    public async Task<string> HandleAsync(
        Dictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!ChatToolHelpers.TryGetString(arguments, "location", out var location) || location is null)
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

        return BuildResponse(location, changed, alreadyCorrect);
    }

    /// <summary>
    /// Builds a user-facing response summarizing the thermostat power operation.
    /// </summary>
    /// <param name="location">The requested thermostat location, or all for every thermostat.</param>
    /// <param name="changed">The number of thermostats changed by the operation.</param>
    /// <param name="alreadyCorrect">The number of thermostats already in the requested power state.</param>
    /// <returns>A message summarizing the operation result.</returns>
    private string BuildResponse(string location, int changed, int alreadyCorrect)
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