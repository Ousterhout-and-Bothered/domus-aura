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
/// A command that performed an implicit power-on is never a no-op — the
/// power-state change itself is a state change.
/// </param>
/// <param name="ImplicitPowerOn">
/// Indicates that the command had to power the device on as part of its work.
/// Set when the command's receiver was off at the start of execution and the
/// command performed a power-on transition before applying its primary effect
/// (e.g., setting brightness on an off light, setting temperature on an off
/// thermostat). The orchestration layer uses this flag to write a paired
/// SetPower history entry, and the UI uses it to render a "powered on
/// automatically" annotation.
/// </param>
/// <param name="ImplicitModeChange">
/// Indicates that the command had to change the device's mode as part of its
/// work. Currently used by <c>SetDesiredTemperatureCommand</c> to signal that
/// the thermostat's mode was switched to Auto so the desired temperature
/// could be reached unconditionally. Like <see cref="ImplicitPowerOn"/>, this
/// flag exists so the audit trail and the UI can faithfully describe what
/// happened rather than glossing over a side effect.
/// </param>
/// <remarks>
/// This type is the core result object for the Command pattern. It is produced
/// by all device commands and consumed by higher-level systems such as scene
/// execution and API mapping.
///
/// A successful command may represent any of:
/// <list type="bullet">
/// <item><description>A pure state change (<c>Success = true</c>, <c>IsNoOp = false</c>,
/// no implicit flags set)</description></item>
/// <item><description>A no-op (<c>Success = true</c>, <c>IsNoOp = true</c>)</description></item>
/// <item><description>A state change that included one or more implicit side effects
/// (<c>Success = true</c>, <c>IsNoOp = false</c>, with <see cref="ImplicitPowerOn"/>
/// and/or <see cref="ImplicitModeChange"/> set)</description></item>
/// </list>
///
/// Implicit-flag combinations are independent: a single command may set both
/// <see cref="ImplicitPowerOn"/> and <see cref="ImplicitModeChange"/> in the
/// same result (e.g., setting a temperature on an Off thermostat that was
/// previously in Heat mode). The UI is responsible for combining them into
/// human-readable language.
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
    bool IsNoOp = false,
    bool ImplicitPowerOn = false,
    bool ImplicitModeChange = false);