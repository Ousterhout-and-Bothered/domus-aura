using SmartHome.Domain.Common;
using SmartHome.Domain.Device;

namespace SmartHome.Domain.Scene;

/// <summary>
/// A single step in a <see cref="DeviceScene"/>: one operation applied to either
/// a specific device or a group of devices matching a type and location.
/// </summary>
/// <remarks>
/// A scene action targets exactly one of:
/// <list type="bullet">
///   <item>a specific device, identified by <see cref="DeviceId"/>, or</item>
///   <item>a group, identified by <see cref="DeviceType"/> + <see cref="Location"/>.</item>
/// </list>
/// Group targets are resolved at execution time against the current device registry,
/// so devices added after scene creation are automatically included.
/// </remarks>
public sealed class SceneAction
{
    public Guid Id { get; private set; }

    /// <summary>Set when this action targets one specific device.</summary>
    public Guid? DeviceId { get; private set; }

    /// <summary>
    /// Set when this action targets a device group. Combine with <see cref="Location"/>
    /// to scope the group to one room, or leave Location null to match all devices
    /// of this type regardless of location.
    /// </summary>
    public DeviceType? DeviceType { get; private set; }

    /// <summary>
    /// Optional scope for a group target. Null means "any location" — the group
    /// matches all devices of the given <see cref="DeviceType"/>.
    /// </summary>
    public string? Location { get; private set; }

    /// <summary>The command name to execute (e.g. "SetBrightness", "Lock").</summary>
    public string Operation { get; private set; } = string.Empty;

    /// <summary>
    /// Optional argument for the operation, as a string. Parameterless operations
    /// (Lock, Unlock) leave this null; single-argument operations (SetBrightness = "50",
    /// SetColor = "#FF0000", SetMode = "Heat") store the argument here. The string is
    /// parsed into the appropriate type by the command factory at execution time.
    /// </summary>
    public string? Value { get; private set; }

    /// <summary>Position within the parent scene's ordered action list. Enforced to equal list position by <see cref="DeviceScene"/>.</summary>
    public int OrderIndex { get; private set; }

    /// <summary>True if this action targets a specific device by id.</summary>
    public bool TargetsDevice => DeviceId.HasValue;

    /// <summary>True if this action targets a device group by type and location.</summary>
    public bool TargetsGroup => DeviceType.HasValue;

    // Required for EF Core rehydration.
    private SceneAction() { }

    private SceneAction(
        Guid? deviceId,
        DeviceType? deviceType,
        string? location,
        string operation,
        string? value,
        int orderIndex)
    {
        // Enforce "exactly one target kind" at the constructor so any future factory
        // cannot accidentally violate the invariant.
        Guard.Against(
            deviceId.HasValue ^ deviceType.HasValue,
            "A scene action must target either a specific device or a device group, not both or neither.");

        Id = Guid.NewGuid();
        DeviceId = deviceId;
        DeviceType = deviceType;
        Location = location;
        Operation = Guard.NotNullOrWhitespace(operation, "Operation is required.");
        Value = value;
        OrderIndex = orderIndex;
    }

    /// <summary>Used by the parent aggregate to renumber actions after collection replacement.</summary>
    internal void SetOrderIndex(int orderIndex) => OrderIndex = orderIndex;

    /// <summary>Creates an action targeting one specific device.</summary>
    public static SceneAction ForDevice(
        Guid deviceId,
        string operation,
        int orderIndex,
        string? value = null) =>
        new(
            deviceId: deviceId,
            deviceType: null,
            location: null,
            operation: operation,
            value: value,
            orderIndex: orderIndex);

    /// <summary>
    /// Creates an action targeting a device group. Resolved at execution time
    /// against the current device registry.
    /// </summary>
    public static SceneAction ForGroup(
        DeviceType deviceType,
        string? location,
        string operation,
        int orderIndex,
        string? value = null) =>
        new(
            deviceId: null,
            deviceType: deviceType,
            location: location,
            operation: operation,
            value: value,
            orderIndex: orderIndex);
}