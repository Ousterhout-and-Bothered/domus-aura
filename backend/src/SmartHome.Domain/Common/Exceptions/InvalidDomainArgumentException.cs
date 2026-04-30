namespace SmartHome.Domain.Common.Exceptions;

/// <summary>
/// Thrown when an invalid argument is provided to a domain method.
/// </summary>
public sealed class InvalidDomainArgumentException(string message) : DomainException(message);
