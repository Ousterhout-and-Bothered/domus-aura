namespace SmartHome.Domain.Common.Exceptions;

/// <summary>
/// Thrown when an attempt is made to register a thermostat in a location that already has one.
/// </summary>
public sealed class DuplicateThermostatException(string location)
    : DomainException($"A thermostat already exists in {location}.")
{
    public string Location { get; } = location;
}
