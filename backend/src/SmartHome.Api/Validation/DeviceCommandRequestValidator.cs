using FluentValidation;
using SmartHome.Api.Contracts.Devices;
using SmartHome.Domain.Common;

namespace SmartHome.Api.Validation;

public sealed class DeviceCommandRequestValidator : AbstractValidator<DeviceCommandRequest>
{
    private static readonly string[] AllowedCommands = 
    [
        "setPower", "setBrightness", "setSpeed", "setMode", "lock", "unlock", "setDesiredTemperature", "setColor"
    ];

    public DeviceCommandRequestValidator()
    {
        RuleFor(x => x.Command)
            .NotEmpty()
            .Must(cmd => AllowedCommands.Contains(cmd, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Command must be one of: {string.Join(", ", AllowedCommands)}");

        RuleFor(x => x.Value)
            .Custom((value, context) =>
            {
                var request = context.InstanceToValidate;
                var normalizedValue = ValueParser.Normalize(value);
                
                if (request.Command.Equals("setBrightness", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue == null)
                    {
                        context.AddFailure("Brightness value is required.");
                        return;
                    }

                    if (int.TryParse(normalizedValue.ToString(), out var brightness))
                    {
                        if (brightness < 10 || brightness > 100)
                        {
                            context.AddFailure("Brightness must be between 10 and 100.");
                        }
                    }
                    else
                    {
                        context.AddFailure("Brightness must be a valid integer.");
                    }
                }

                if (request.Command.Equals("setPower", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue == null)
                    {
                        context.AddFailure("Power state value is required.");
                        return;
                    }

                    var val = normalizedValue.ToString();
                    if (!Enum.TryParse<SmartHome.Domain.Device.PowerState>(val, true, out _))
                    {
                        context.AddFailure($"Power state must be one of: {string.Join(", ", Enum.GetNames<SmartHome.Domain.Device.PowerState>())}.");
                    }
                }

                if (request.Command.Equals("setSpeed", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue == null)
                    {
                        context.AddFailure("Speed value is required.");
                        return;
                    }

                    var val = normalizedValue.ToString();
                    if (!Enum.TryParse<SmartHome.Domain.Device.Fan.FanSpeed>(val, true, out _))
                    {
                        context.AddFailure($"Fan speed must be one of: {string.Join(", ", Enum.GetNames<SmartHome.Domain.Device.Fan.FanSpeed>())}.");
                    }
                }

                if (request.Command.Equals("setMode", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue == null)
                    {
                        context.AddFailure("Mode value is required.");
                        return;
                    }

                    var val = normalizedValue.ToString();
                    if (!Enum.TryParse<SmartHome.Domain.Device.Thermostat.ThermostatMode>(val, true, out _))
                    {
                        context.AddFailure($"Thermostat mode must be one of: {string.Join(", ", Enum.GetNames<SmartHome.Domain.Device.Thermostat.ThermostatMode>())}.");
                    }
                }

                if (request.Command.Equals("setDesiredTemperature", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue == null)
                    {
                        context.AddFailure("Temperature value is required.");
                        return;
                    }

                    if (int.TryParse(normalizedValue.ToString(), out var temp))
                    {
                        if (temp < 60 || temp > 80)
                        {
                            context.AddFailure("Temperature must be between 60 and 80.");
                        }
                    }
                    else
                    {
                        context.AddFailure("Temperature must be a valid integer.");
                    }
                }

                if (request.Command.Equals("setColor", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue == null)
                    {
                        context.AddFailure("Color value is required.");
                        return;
                    }

                    var color = normalizedValue.ToString()!;
                    if (!System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$"))
                    {
                        context.AddFailure("Color must be a valid hex color (e.g., #FFFFFF).");
                    }
                }
            });
    }
}
