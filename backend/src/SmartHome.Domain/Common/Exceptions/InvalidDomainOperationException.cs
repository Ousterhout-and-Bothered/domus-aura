namespace SmartHome.Domain.Common.Exceptions;

/// <summary>
/// Thrown when an operation is invalid in the current state of the domain object.
/// </summary>
public sealed class InvalidDomainOperationException(string message) : DomainException(message);
