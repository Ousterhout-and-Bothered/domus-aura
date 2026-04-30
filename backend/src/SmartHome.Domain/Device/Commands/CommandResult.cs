namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Represents the outcome of executing a device command.
/// Contains both the structured result (success/failure/no-op)
/// and the metadata needed to describe what was attempted.
/// </summary>
/// <param name="DeviceId">The unique identifier of the device targeted by the command.</param>
/// <param name="DeviceName">The human-readable name of the device.</param>
/// <param name="DeviceType">The type of device the command was executed against.</param>
/// <param name="Operation">The operation performed (e.g., "SetPower", "Lock").</param>
/// <param name="Value">
/// The value associated with the operation, if applicable
/// (e.g., "On", "Off", "50", "#FF0000"). Null for parameterless operations.
/// </param>
/// <param name="Success">
/// Indicates whether the command completed successfully. This includes
/// both state changes and no-op operations where the device was already
/// in the requested state.
/// </param>
/// <param name="Message">
/// Optional human-readable message describing the outcome. Typically used
/// for failures or no-op explanations (e.g., "Device is already in the requested state.").
/// </param>
/// <param name="IsNoOp">
/// Indicates that the command resulted in no state change because the device
/// was already in the requested state. No-op commands are considered successful
/// but are distinguished from actual state changes for reporting purposes.
/// </param>
/// <remarks>
/// This type is the core result object for the Command pattern. It is produced
/// by all device commands and consumed by higher-level systems such as scene
/// execution and API mapping.
///
/// A successful command may represent either:
/// <list type="bullet">
/// <item><description>A state change (<c>Success = true</c>, <c>IsNoOp = false</c>)</description></item>
/// <item><description>A no-op (<c>Success = true</c>, <c>IsNoOp = true</c>)</description></item>
/// </list>
///
/// Failures are represented by:
/// <list type="bullet">
/// <item><description><c>Success = false</c>, with an explanatory <c>Message</c></description></item>
/// </list>
/// </remarks>
public sealed record CommandResult(
    Guid DeviceId,
    string DeviceName,
    DeviceType DeviceType,
    string Operation,
    string? Value,
    bool Success,
    string? Message,
    bool IsNoOp = false);