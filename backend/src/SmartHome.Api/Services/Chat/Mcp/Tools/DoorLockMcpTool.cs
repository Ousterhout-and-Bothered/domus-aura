using System.ComponentModel;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.DoorLock;
using ModelContextProtocol.Server;

namespace SmartHome.Api.Services.Chat.Mcp.Tools;

/// <summary>
/// Handles chat tool requests for locking or unlocking door lock devices.
/// </summary>
/// <param name="deviceService">The service used to retrieve devices and execute door lock commands.</param>
[McpServerToolType]
public sealed class DoorLockTool(
    IDeviceService deviceService)
{
    /// <summary>
    /// Gets the tool name exposed to the language model for locking doors.
    /// </summary>
    public const string LockToolName = "lock_door";

    /// <summary>
    /// Gets the tool name exposed to the language model for unlocking doors.
    /// </summary>
    public const string UnlockToolName = "unlock_door";

    /// <summary>
    /// Locks a door by name, or uses all to lock every door.
    /// </summary>
    /// <param name="name">Door name like Front Door or Back Door, or all.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the door lock operation.</returns>
    [McpServerTool(Name = LockToolName)]
    [Description("Lock a door by name, or use 'all' to lock every door.")]
    public Task<string> LockDoorAsync(
        [Description("Door name like Front Door or Back Door, or all.")]
        string name,
        CancellationToken cancellationToken = default)
    {
        return HandleAsync(name, true, cancellationToken);
    }

    /// <summary>
    /// Unlocks a door by name, or uses all to unlock every door.
    /// </summary>
    /// <param name="name">Door name like Front Door or Back Door, or all.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the door lock operation.</returns>
    [McpServerTool(Name = UnlockToolName)]
    [Description("Unlock a door by name, or use 'all' to unlock every door.")]
    public Task<string> UnlockDoorAsync(
        [Description("Door name like Front Door or Back Door, or all.")]
        string name,
        CancellationToken cancellationToken = default)
    {
        return HandleAsync(name, false, cancellationToken);
    }

    /// <summary>
    /// Executes the door lock tool using the supplied model arguments.
    /// </summary>
    /// <param name="doorName">The door name parsed from the model's tool call.</param>
    /// <param name="shouldLock">A value indicating whether this handler locks or unlocks doors.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A message describing the result of the door lock operation.</returns>
    private async Task<string> HandleAsync(
        string doorName,
        bool shouldLock,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(doorName))
        {
            return "I need a door name to control a door lock.";
        }

        var doors = await deviceService.GetAllDevicesAsync(
            null,
            DeviceType.DoorLock,
            null,
            cancellationToken);

        var targetDoors = ChatToolHelpers.IsAll(doorName)
            ? doors.ToList()
            : doors
                .Where(d => string.Equals(d.Name, doorName, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (targetDoors.Count == 0)
        {
            return ChatToolHelpers.IsAll(doorName)
                ? "No doors were found."
                : $"I could not find a door named {doorName}.";
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

        return BuildDoorResponse(doorName, shouldLock, changed, alreadyCorrect);
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