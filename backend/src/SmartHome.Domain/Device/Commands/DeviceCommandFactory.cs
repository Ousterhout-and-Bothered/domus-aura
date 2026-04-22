using SmartHome.Domain.Common;
using SmartHome.Domain.Common.Exceptions;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Thermostat;
using SmartHome.Domain.Device.Light;
using SmartHome.Domain.Device.DoorLock;

namespace SmartHome.Domain.Device.Commands;

/// <summary>
/// Default implementation of <see cref="IDeviceCommandFactory"/>.
/// Maps command names and values to concrete command objects.
/// </summary>
public sealed class DeviceCommandFactory : IDeviceCommandFactory
{
    /// <inheritdoc />
    public IDeviceCommand Create(string commandName, object? value, Device device)
    {
        var normalizedCommand = commandName.ToLowerInvariant();

        return (normalizedCommand, device) switch
        {
            ("setpower", IPowerable p) => new SetPowerCommand(p, ParseEnum<PowerState>(value)),
            ("setbrightness", IDimmable d) => new SetBrightnessCommand(d, ParseInt(value)),
            ("setcolor", IColorable c) => new SetColorCommand(c, value?.ToString() ?? string.Empty),
            ("setspeed", IFanControllable f) => new SetSpeedCommand(f, ParseEnum<FanSpeed>(value)),
            ("setmode", IThermostatControllable t) => new SetModeCommand(t, ParseEnum<ThermostatMode>(value)),
            ("setdesiredtemperature", IThermostatControllable t) => new SetDesiredTemperatureCommand(t, ParseInt(value)),
            ("lock", ILockable l) => new LockCommand(l),
            ("unlock", ILockable l) => new UnlockCommand(l),
            
            _ when !IsCommandKnown(normalizedCommand) 
                => throw new InvalidDomainArgumentException($"Unknown command: {commandName}"),
            _ => throw new InvalidDomainOperationException($"Command '{commandName}' is not supported by device type '{device.Type}'.")
        };
    }

    private static bool IsCommandKnown(string commandName) => commandName switch
    {
        "setpower" or "setbrightness" or "setcolor" or "setspeed" or 
        "setmode" or "setdesiredtemperature" or "lock" or "unlock" => true,
        _ => false
    };

    /// <summary>
    /// Attempts to parse a value into a specific enum type.
    /// </summary>
    /// <typeparam name="T">The target enum type.</typeparam>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed enum value.</returns>
    /// <exception cref="InvalidDomainArgumentException">
    /// Thrown if the value is null, not a string, or not a valid enum member.
    /// </exception>
    private static T ParseEnum<T>(object? value) where T : struct, Enum
    {
        var allowedValues = Guard.GetAllowedValues<T>();
        
        var stringValue = value switch
        {
            null => throw new InvalidDomainArgumentException($"Value is required for this command. Allowed values: {allowedValues}"),
            string s => s,
            _ => value.ToString() ?? throw new InvalidDomainArgumentException($"Value must be a string for enum parsing. Allowed values: {allowedValues}")
        };

        if (Enum.TryParse<T>(stringValue, true, out var result))
            return result;

        throw new InvalidDomainArgumentException($"Unsupported {typeof(T).Name} value. Allowed values: {allowedValues}");
    }

    /// <summary>
    /// Attempts to parse a value into an integer.
    /// Handles both direct numeric types and <see cref="JsonElement"/> numeric values.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed integer value.</returns>
    /// <exception cref="InvalidDomainArgumentException">
    /// Thrown if the value cannot be parsed as an integer.
    /// </exception>
    private static int ParseInt(object? value)
    {
        return ValueParser.TryParseInt(value) ?? throw new InvalidDomainArgumentException("Value must be a valid number.");
    }
}
