using SmartHome.Domain.Device.Commands;
using SmartHome.Domain.Common.Exceptions;

namespace SmartHome.Domain.Scene;

/// <summary>
/// A composite of <see cref="IDeviceCommand"/>s that executes its children in order
/// and collects their results. Represents the "many commands as one unit" step
/// in scene execution.
/// </summary>
/// <remarks>
/// <para>
/// This type is the Composite pattern as applied to scenes. Unlike the canonical GoF form,
/// it does NOT implement <see cref="IDeviceCommand"/>: the single-command interface returns
/// one <see cref="CommandResult"/>, but a composite genuinely produces many. Forcing the
/// composite into the single-command interface would either lose per-action detail or
/// require fabricating a fake summary result. The two use cases (single-device execution
/// vs. scene execution) are not interchangeable callers, so the interface is intentionally
/// separate.
/// </para>
/// <para>
/// Execution is tolerant of partial failure: if one child throws, the composite catches
/// the exception, records it as a failed <see cref="CommandResult"/>, and continues to
/// the next child. This is the spec's "scene does not abort on partial failure" requirement.
/// </para>
/// </remarks>
public sealed class CompositeCommand
{
    private readonly List<IDeviceCommand> _children = [];

    /// <summary>The child commands in execution order.</summary>
    public IReadOnlyList<IDeviceCommand> Children => _children;

    /// <summary>Adds a child command to this composite. Children execute in the order added.</summary>
    public void Add(IDeviceCommand child) => _children.Add(child);

    /// <summary>
    /// Executes each child in order, collecting results. A child that throws a domain
    /// exception contributes a failed <see cref="CommandResult"/> to the output; execution
    /// continues with the next child regardless.
    /// </summary>
    /// <returns>One <see cref="CommandResult"/> per child, in execution order.</returns>
    public IReadOnlyList<CommandResult> Execute()
    {
        var results = new List<CommandResult>(_children.Count);

        foreach (var child in _children)
        {
            try
            {
                results.Add(child.Execute());
            }
            catch (DomainException ex)
            {
                // Composite pattern requirement: partial-failure tolerance.
                // A failing child command must not abort sibling execution.
                // DomainException is caught narrowly; unexpected runtime errors
                // still propagate so they fail loudly rather than silently.
                results.Add(new CommandResult(
                    DeviceId: child.DeviceId ?? Guid.Empty,
                    DeviceName: child.DeviceName ?? "Unknown",
                    DeviceType: child.DeviceType ?? default,
                    Operation: child.OperationName,
                    Value: child.Value,
                    Success: false,
                    Message: ex.Message));
            }
        }

        return results;
    }
}