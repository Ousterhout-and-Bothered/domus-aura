using SmartHome.Domain.Simulation;

namespace SmartHome.Api.Contracts;

/// <summary>
/// The set of simulation speeds currently permitted by the registry.
/// Intended for frontend dropdowns and speed-picker UIs.
/// </summary>
/// <param name="Speeds">Permitted speed multipliers.</param>
public sealed record AllowedSpeedsResponse(
    IReadOnlyCollection<int> Speeds);