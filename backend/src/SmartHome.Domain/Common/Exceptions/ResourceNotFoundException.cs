namespace SmartHome.Domain.Common.Exceptions;

/// <summary>
/// Thrown when a requested device is not found.
/// </summary>
public sealed class ResourceNotFoundException(string message) : DomainException(message);
