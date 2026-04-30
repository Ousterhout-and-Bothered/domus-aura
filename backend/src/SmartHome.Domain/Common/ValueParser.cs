using System.Text.Json;

namespace SmartHome.Domain.Common;

/// <summary>
/// Provides utility methods for parsing values from various formats,
/// including direct numeric types, strings, and JsonElement values.
/// </summary>
public static class ValueParser
{
    /// <summary>
    /// Attempts to parse an object into an integer.
    /// Handles direct numeric types, strings, and JsonElement values.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed integer value if successful; otherwise null.</returns>
    public static int? TryParseInt(object? value) => Normalize(value) switch
    {
        null => null,
        int i => i,
        string s when int.TryParse(s, out var result) => result,
        _ => TryConvert(value)
    };

    /// <summary>
    /// Normalizes a raw object value into its underlying primitive type.
    /// Specifically handles extracting values from <see cref="JsonElement"/> wrappers.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>The underlying value if it is a primitive type, string, or null; otherwise the original value.</returns>
    public static object? Normalize(object? value)
    {
        if (value is not JsonElement element)
        {
            return value;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value
        };
    }

    private static int? TryConvert(object? value)
    {
        if (value is null)
            return null;

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return null;
        }
    }
}
