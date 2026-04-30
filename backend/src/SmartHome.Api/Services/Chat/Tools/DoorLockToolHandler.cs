using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;
using System.Text.Json;

namespace SmartHome.Api.Services.Chat.Tools;

/// <summary>
/// Handles chat tool requests for locking or unlocking door lock devices.
/// </summary>
/// <param name="deviceService">The service used to retrieve devices and execute door lock commands.</param>
/// <param name="shouldLock">A value indicating whether this handler locks or unlocks doors.</param>
public sealed class DoorLockToolHandler(
    IDeviceService deviceService,
    bool shouldLock) : IChatToolHandler
{
    /// <summary>
    /// Gets the tool name exposed to the language model.
    /// </summary>
    public string ToolName => shouldLock ? "lock_door" : "unlock_door";

    /// <summary>
    /// Gets the tool definition sent to the language model.
    /// </summary>
    public object ToolDefinition => new
    {
        type = "function",
        function = new
        {
            name = ToolName,
            description = shouldLock
                ? "Lock a door by name, or use 'all' to lock every door."
                : "Unlock a door by name, or use 'all' to unlock every door.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    name = new
                    {
                        type = "string",
                        description = "Door name like Front Door or Back Door, or all"
                    }
                },
                required = new[] { "name" }
            }
        }
    };

    /// <summary>
    /// Executes the door lock tool using the supplied model arguments.
    /// </summary>
    /// <param name="arguments">The tool arguments parsed from the model's tool call.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the door lock operation.</returns>
    public async Task<string> HandleAsync(
        Dictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!ChatToolHelpers.TryGetString(arguments, "name", out var doorName))
        {
            return "I need a door name to control a door lock.";
        }

        var doors = await deviceService.GetAllDevicesAsync(
            null,
            DeviceType.DoorLock,
            null,
            cancellationToken);

        var targetDoors = ChatToolHelpers.IsAll(doorName!)
            ? doors.ToList()
            : doors
                .Where(d => string.Equals(d.Name, doorName, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (targetDoors.Count == 0)
        {
            return $"I could not find a door named {doorName}.";
        }

        var changed = 0;
        var alreadyCorrect = 0;

        foreach (var door in targetDoors)
        {
            var doorLock = (DoorLock)door;

            var isAlreadyCorrect = shouldLock
                ? doorLock.LockState == DoorLockState.Locked
                : doorLock.LockState == DoorLockState.Unlocked;

            if (isAlreadyCorrect)
            {
                alreadyCorrect++;
                continue;
            }

            await deviceService.ExecuteCommandAsync(
                door.Id,
                shouldLock ? "Lock" : "Unlock",
                null,
                cancellationToken);

            changed++;
        }

        return BuildDoorResponse(doorName!, shouldLock, changed, alreadyCorrect);
    }

    /// <summary>
    /// Builds a user-facing response summarizing the door lock operation.
    /// </summary>
    /// <param name="doorName">The requested door name, or all for every door.</param>
    /// <param name="shouldLock">A value indicating whether the requested operation was lock or unlock.</param>
    /// <param name="changed">The number of doors changed by the operation.</param>
    /// <param name="alreadyCorrect">The number of doors already in the requested state.</param>
    /// <returns>A message summarizing the operation result.</returns>
    private static string BuildDoorResponse(
        string doorName,
        bool shouldLock,
        int changed,
        int alreadyCorrect)
    {
        var allDoors = ChatToolHelpers.IsAll(doorName);
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
            return $"All {ChatToolHelpers.Pluralize(alreadyCorrect, "door")} were already {state}.";
        }

        if (alreadyCorrect == 0)
        {
            return $"{action} {ChatToolHelpers.Pluralize(changed, "door")}.";
        }

        return $"{action} {ChatToolHelpers.Pluralize(changed, "door")}. {ChatToolHelpers.SentenceCount(alreadyCorrect, "door")} already {state}.";
    }
}