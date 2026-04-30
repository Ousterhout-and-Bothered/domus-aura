using SmartHome.Domain.Common.Exceptions;

namespace SmartHome.Domain.Common;

/// <summary>
/// Provides common validation and exception helpers to ensure consistency 
/// and adhere to DRY principles across the domain.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Ensures the specified enum value is defined.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <param name="messagePrefix">Optional custom prefix for the error message.</param>
    /// <exception cref="InvalidDomainArgumentException">Thrown if the value is not defined in the enum.</exception>
    public static void EnumDefined<T>(T value, string? parameterName = null, string? messagePrefix = null) where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            var allowedValues = GetAllowedValues<T>();
            var prefix = messagePrefix ?? $"Unsupported {typeof(T).Name} value.";
            var message = $"{prefix} Allowed values: {allowedValues}";

            // To provide the exact message requested by the user in the API response,
            // we throw InvalidDomainArgumentException with only the message. 
            throw new InvalidDomainArgumentException(message);
        }
    }

    /// <summary>
    /// Gets a comma-separated string of all defined names for an enum type.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <returns>A comma-separated list of allowed enum names.</returns>
    public static string GetAllowedValues<T>() where T : struct, Enum
    {
        return string.Join(", ", Enum.GetNames<T>());
    }

    /// <summary>
    /// Ensures the specified string is not null, empty, or whitespace.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="message">The exception message.</param>
    /// <returns>The trimmed string if valid.</returns>
    /// <exception cref="InvalidDomainArgumentException">Thrown if the string is null or whitespace.</exception>
    public static string NotNullOrWhitespace(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDomainArgumentException(message);

        return value.Trim();
    }

    /// <summary>
    /// Ensures the specified value is within the given range (inclusive).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <param name="message">The exception message.</param>
    /// <exception cref="InvalidDomainArgumentException">Thrown if the value is out of range.</exception>
    public static void InRange(int value, int min, int max, string message)
    {
        if (value < min || value > max)
            throw new InvalidDomainArgumentException(message);
    }

    /// <summary>
    /// Ensures the specified condition is met, otherwise throws an <see cref="InvalidDomainArgumentException"/>.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="message">The exception message.</param>
    /// <exception cref="InvalidDomainArgumentException">Thrown if the condition is false.</exception>
    public static void Against(bool condition, string message)
    {
        if (!condition)
            throw new InvalidDomainArgumentException(message);
    }

    /// <summary>
    /// Ensures the condition is met, otherwise throws an <see cref="InvalidDomainOperationException"/>.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="message">The exception message.</param>
    /// <exception cref="InvalidDomainOperationException">Thrown if the condition is false.</exception>
    public static void AgainstInvalidState(bool condition, string message)
    {
        if (!condition)
            throw new InvalidDomainOperationException(message);
    }

    /// <summary>
    /// Ensures the specified transition is legal for the given state machine.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="currentState">The current state.</param>
    /// <param name="targetState">The desired target state.</param>
    /// <param name="allowedTransitions">The transition table.</param>
    /// <exception cref="InvalidDomainOperationException">Thrown if the transition is not allowed.</exception>
    public static void ThrowIfInvalidTransition<TState>(
        TState currentState,
        TState targetState,
        IReadOnlyDictionary<TState, IReadOnlySet<TState>> allowedTransitions) where TState : struct
    {
        var detail = allowedTransitions.TryGetValue(currentState, out var allowed)
            ? $"Allowed transitions from {currentState}: {string.Join(", ", allowed)}."
            : $"No transitions are allowed from the current state {currentState}.";

        throw new InvalidDomainOperationException(
            $"Invalid transition: {currentState} -> {targetState}. {detail}");
    }

    /// <summary>
    /// Clamps a value to the specified inclusive range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <returns>The clamped value.</returns>
    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}