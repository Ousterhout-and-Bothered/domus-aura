namespace SmartHome.Api.Mapping;

/// <summary>
/// Maps HTTP query parameters into device filter values.
/// </summary>
public static class DeviceFilterMapper
{
    /// <summary>
    /// Converts a query string state value into a nullable power filter.
    /// </summary>
    /// <param name="state">The query state value.</param>
    /// <returns>
    /// True for "on", false for "off", otherwise null.
    /// </returns>
    public static bool? MapState(string? state)
    {
        return state?.ToLowerInvariant() switch
        {
            "on" => true,
            "off" => false,
            _ => null
        };
    }
}