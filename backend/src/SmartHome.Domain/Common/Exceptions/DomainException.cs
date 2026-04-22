namespace SmartHome.Domain.Common.Exceptions;

/// <summary>
/// Base class for all custom domain exceptions. 
/// Using a base class allows the global exception handler to distinguish between 
/// controlled domain errors and unexpected framework/infrastructure errors.
/// </summary>
public abstract class DomainException(string message, Exception? innerException = null) 
    : Exception(message, innerException);
