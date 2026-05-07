using SmartHome.Domain.Common;
using SmartHome.Domain.Common.Exceptions;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Provides shared parsing helpers for values passed to device commands.
/// </summary>
internal static class CommandValueParser
{
    /// <summary>
    /// Parses a command value into a supported enum value.
    /// </summary>
    /// <typeparam name="T">The enum type to parse.</typeparam>
    /// <param name="value">The raw command value.</param>
    /// <returns>The parsed enum value.</returns>
    /// <exception cref="InvalidDomainArgumentException">
    /// Thrown when the value is missing or does not match a defined enum value.
    /// </exception>
    public static T ParseEnum<T>(object? value) where T : struct, Enum
    {
        var allowedValues = Guard.GetAllowedValues<T>();

        var stringValue = value switch
        {
            null => throw new InvalidDomainArgumentException(
                $"Value is required for this command. Allowed values: {allowedValues}"),
            string s => s,
            _ => value.ToString() ??
                 throw new InvalidDomainArgumentException(
                     $"Value must be a string for enum parsing. Allowed values: {allowedValues}")
        };

        if (Enum.TryParse<T>(stringValue, true, out var result) && Enum.IsDefined(result))
        {
            return result;
        }

        throw new InvalidDomainArgumentException(
            $"Unsupported {typeof(T).Name} value. Allowed values: {allowedValues}");
    }

    /// <summary>
    /// Parses a command value into an integer.
    /// </summary>
    /// <param name="value">The raw command value.</param>
    /// <returns>The parsed integer value.</returns>
    /// <exception cref="InvalidDomainArgumentException">
    /// Thrown when the value cannot be parsed as a valid integer.
    /// </exception>
    public static int ParseInt(object? value)
    {
        return ValueParser.TryParseInt(value)
            ?? throw new InvalidDomainArgumentException("Value must be a valid number.");
    }
}