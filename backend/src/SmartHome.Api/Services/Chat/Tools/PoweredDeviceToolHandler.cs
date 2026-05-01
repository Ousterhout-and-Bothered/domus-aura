using SmartHome.Domain.Device;
using System.Text.Json;

namespace SmartHome.Api.Services.Chat.Tools;

/// <summary>
/// Handles chat tool requests for turning powered devices on or off by location or across all matching devices.
/// </summary>
/// <param name="deviceService">The service used to retrieve devices and execute power commands.</param>
/// <param name="deviceType">The type of powered device targeted by this handler.</param>
/// <param name="deviceLabel">The readable device label used in tool names and responses.</param>
/// <param name="turnOn">A value indicating whether this handler turns devices on or off.</param>
public sealed class PoweredDeviceToolHandler(
    IDeviceService deviceService,
    DeviceType deviceType,
    string deviceLabel,
    bool turnOn) : IChatToolHandler
{
    /// <summary>
    /// Gets the tool name exposed to the language model.
    /// </summary>
    public string ToolName => $"turn_{(turnOn ? "on" : "off")}_{deviceLabel}s";

    /// <summary>
    /// Gets the tool definition sent to the language model.
    /// </summary>
    public object ToolDefinition => new
    {
        type = "function",
        function = new
        {
            name = ToolName,
            description = $"Turn {(turnOn ? "on" : "off")} {deviceLabel}s in a location, or use 'all' to target every {deviceLabel}.",
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
    /// Executes the powered device tool using the supplied model arguments.
    /// </summary>
    /// <param name="arguments">The tool arguments parsed from the model's tool call.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the power operation.</returns>
    public async Task<string> HandleAsync(
        Dictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!ChatToolHelpers.TryGetString(arguments, "location", out var location) || location is null)
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

        return BuildResponse(location, changed, alreadyCorrect);
    }

    /// <summary>
    /// Builds a user-facing response summarizing the powered device operation.
    /// </summary>
    /// <param name="location">The requested device location, or all for every matching device.</param>
    /// <param name="changed">The number of devices changed by the operation.</param>
    /// <param name="alreadyCorrect">The number of devices already in the requested power state.</param>
    /// <returns>A message summarizing the operation result.</returns>
    private string BuildResponse(string location, int changed, int alreadyCorrect)
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