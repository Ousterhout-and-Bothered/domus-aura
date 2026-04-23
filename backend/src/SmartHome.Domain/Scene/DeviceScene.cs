using SmartHome.Domain.Common;

namespace SmartHome.Domain.Scene;

/// <summary>
/// A named preset describing an ordered list of device operations to execute together.
/// Aggregate root for the scene feature.
/// </summary>
/// <remarks>
/// A scene is a definition, not a behavior: it describes *what should happen*, not *how*.
/// Execution is the responsibility of the scene resolver and executor, which turn this
/// definition into a command tree at runtime.
/// </remarks>
public sealed class DeviceScene
{
    private readonly List<SceneAction> _actions = [];

    public Guid Id { get; private set; }

    /// <summary>The user-facing name of the scene (e.g. "Good Night").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The ordered list of actions this scene executes. Exposed read-only;
    /// the aggregate replaces the collection wholesale via <see cref="ReplaceActions"/>.
    /// Ordering (OrderIndex == list position) is established on construction and mutation,
    /// and restored by the repository on load.
    /// </summary>
    public IReadOnlyList<SceneAction> Actions => _actions;

    // Required for EF Core rehydration.
    private DeviceScene() { }

    /// <summary>
    /// Creates a new scene with the given name and actions.
    /// </summary>
    /// <exception cref="Common.Exceptions.InvalidDomainArgumentException">
    /// Thrown if the name is null/whitespace or the action list is empty.
    /// </exception>
    public DeviceScene(string name, IEnumerable<SceneAction> actions)
    {
        Id = Guid.NewGuid();
        Name = Guard.NotNullOrWhitespace(name, "Scene name is required.");

        var ordered = actions.ToList();
        Guard.Against(ordered.Count > 0, "A scene must contain at least one action.");

        RenumberAndStore(ordered);
    }

    /// <summary>
    /// Renames the scene.
    /// </summary>
    public void Rename(string newName)
    {
        Name = Guard.NotNullOrWhitespace(newName, "Scene name is required.");
    }

    /// <summary>
    /// Replaces the action list wholesale. Used when the user edits a scene —
    /// old actions are discarded and replaced by the new collection.
    /// </summary>
    /// <exception cref="Common.Exceptions.InvalidDomainArgumentException">
    /// Thrown if the new action list is empty.
    /// </exception>
    public void ReplaceActions(IEnumerable<SceneAction> newActions)
    {
        var ordered = newActions.ToList();
        Guard.Against(ordered.Count > 0, "A scene must contain at least one action.");

        _actions.Clear();
        RenumberAndStore(ordered);
    }

    private void RenumberAndStore(List<SceneAction> ordered)
    {
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].SetOrderIndex(i);

        _actions.AddRange(ordered);
    }
}