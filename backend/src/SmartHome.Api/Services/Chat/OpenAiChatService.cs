using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Api.Services.Chat;

public sealed class OpenAiChatService(
    HttpClient httpClient,
    IConfiguration configuration,
    IDeviceService deviceService) : ILlmChatService
{
    private readonly IDeviceService _deviceService = deviceService;

    public async Task<string> GetResponseAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        var model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = "You are a smart home assistant. Use tools to control devices. " +
                                                 "If the user asks for multiple actions, call all necessary tools " +
                                                 "in the same response." },                
                new { role = "user", content = message }
            },
            tools = new object[]
            {
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "turn_on_lights",
                        description = "Turn on lights in a location, or use 'all' to turn on every light.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                location = new { type = "string", description = "Room name like Living Room, or all" }
                            },
                            required = new[] { "location" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "turn_off_lights",
                        description = "Turn off lights in a location, or use 'all' to turn off every light.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                location = new { type = "string", description = "Room name like Living Room, or all" }
                            },
                            required = new[] { "location" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "lock_door",
                        description = "Lock a door by name, or use 'all' to lock every door.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                name = new { type = "string", description = "Door name like Front Door or Back Door, or all" }
                            },
                            required = new[] { "name" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "unlock_door",
                        description = "Unlock a door by name, or use 'all' to unlock every door.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                name = new { type = "string", description = "Door name like Front Door or Back Door, or all" }
                            },
                            required = new[] { "name" }
                        }
                    }
                }
            },
            tool_choice = "auto"
        };

        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI request failed: {response.StatusCode}. Body: {json}");
        }

        using var document = JsonDocument.Parse(json);

        var messageElement = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message");

        if (messageElement.TryGetProperty("tool_calls", out var toolCalls))
{
    var results = new List<string>();

    foreach (var toolCall in toolCalls.EnumerateArray())
    {
        var functionName = toolCall.GetProperty("function").GetProperty("name").GetString();
        var argumentsJson = toolCall.GetProperty("function").GetProperty("arguments").GetString();
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(argumentsJson!);

        if ((functionName == "turn_on_lights" || functionName == "turn_off_lights") &&
            args is not null &&
            args.TryGetValue("location", out var lightLocation))
        {
            var turnOn = functionName == "turn_on_lights";

            var targetLights = (await _deviceService.GetAllDevicesAsync(
                lightLocation.Equals("all", StringComparison.OrdinalIgnoreCase) ? null : lightLocation,
                DeviceType.Light,
                null,
                cancellationToken)).ToList();

            var changed = 0;
            var alreadyInRequestedState = 0;

            foreach (var light in targetLights)
            {
                var isAlreadyCorrect = turnOn ? light.IsOn() : !light.IsOn();

                if (isAlreadyCorrect)
                {
                    alreadyInRequestedState++;
                    continue;
                }

                await _deviceService.ExecuteCommandAsync(
                    light.Id,
                    "SetPower",
                    turnOn ? "On" : "Off",
                    cancellationToken);

                changed++;
            }

            results.Add(BuildLightResponse(lightLocation, turnOn, changed, alreadyInRequestedState));
            continue;
        }

        if ((functionName == "lock_door" || functionName == "unlock_door") &&
            args is not null &&
            args.TryGetValue("name", out var doorName))
        {
            var shouldLock = functionName == "lock_door";

            var doors = await _deviceService.GetAllDevicesAsync(
                null,
                DeviceType.DoorLock,
                null,
                cancellationToken);

            var targetDoors = doorName.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? doors.ToList()
                : doors.Where(d => string.Equals(d.Name, doorName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (targetDoors.Count == 0)
            {
                results.Add($"I could not find a door named {doorName}.");
                continue;
            }

            var changed = 0;
            var alreadyInRequestedState = 0;

            foreach (var door in targetDoors)
            {
                var doorLock = (DoorLock)door;

                var isAlreadyCorrect = shouldLock
                    ? doorLock.LockState == DoorLockState.Locked
                    : doorLock.LockState == DoorLockState.Unlocked;

                if (isAlreadyCorrect)
                {
                    alreadyInRequestedState++;
                    continue;
                }

                await _deviceService.ExecuteCommandAsync(
                    door.Id,
                    shouldLock ? "Lock" : "Unlock",
                    null,
                    cancellationToken);

                changed++;
            }

            results.Add(BuildDoorResponse(doorName, shouldLock, changed, alreadyInRequestedState));
            continue;
        }

        results.Add($"I received an unsupported tool request: {functionName}.");
    }

    return string.Join(" ", results);
}

        return messageElement.GetProperty("content").GetString() ?? "No response";
    }

    private static string BuildLightResponse(string location, bool turnOn, int changed, int alreadyCorrect)
    {
        var allLights = location.Equals("all", StringComparison.OrdinalIgnoreCase);
        var action = turnOn ? "Turned on" : "Turned off";
        var state = turnOn ? "on" : "off";

        if (changed == 0)
        {
            return allLights
                ? $"All {Pluralize(alreadyCorrect, "light")} were already {state}."
                : $"All {Pluralize(alreadyCorrect, "light")} in {location} were already {state}.";
        }

        if (alreadyCorrect == 0)
        {
            return allLights
                ? $"{action} {Pluralize(changed, "light")}."
                : $"{action} {Pluralize(changed, "light")} in {location}.";
        }

        return allLights
            ? $"{action} {Pluralize(changed, "light")}. {SentenceCount(alreadyCorrect, "light")} already {state}."
            : $"{action} {Pluralize(changed, "light")} in {location}. {SentenceCount(alreadyCorrect, "light")} already {state}.";
    }

    private static string BuildDoorResponse(string doorName, bool shouldLock, int changed, int alreadyCorrect)
    {
        var allDoors = doorName.Equals("all", StringComparison.OrdinalIgnoreCase);
        var action = shouldLock ? "Locked" : "Unlocked";
        var state = shouldLock ? "locked" : "unlocked";

        if (!allDoors)
        {
            return changed == 0
                ? $"{doorName} was already {state}."
                : $"{action} {doorName}.";
        }

        if (changed == 0)
        {
            return $"All {Pluralize(alreadyCorrect, "door")} were already {state}.";
        }

        if (alreadyCorrect == 0)
        {
            return $"{action} {Pluralize(changed, "door")}.";
        }

        return $"{action} {Pluralize(changed, "door")}. {SentenceCount(alreadyCorrect, "door")} already {state}.";
    }

    private static string Pluralize(int count, string noun) =>
        count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    private static string SentenceCount(int count, string noun) =>
        count == 1 ? $"1 {noun} was" : $"{count} {noun}s were";
}