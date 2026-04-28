using FluentValidation;
using SmartHome.Api.Contracts.Devices;
using SmartHome.Domain.Common;
using SmartHome.Domain.Device;
using SmartHome.Domain.Device.Fan;
using SmartHome.Domain.Device.Thermostat;

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
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Command is required.")
            .Must(cmd => AllowedCommands.Contains(cmd, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Command must be one of: {string.Join(", ", AllowedCommands)}");

        RuleFor(x => x.Value)
            .Custom((value, context) =>
            {
                var request = context.InstanceToValidate;

                if (string.IsNullOrWhiteSpace(request.Command))
                {
                    return;
                }

                if (!AllowedCommands.Contains(request.Command, StringComparer.OrdinalIgnoreCase))
                {
                    return;
                }

                var normalizedValue = ValueParser.Normalize(value);

                if (request.Command.Equals("lock", StringComparison.OrdinalIgnoreCase) ||
                    request.Command.Equals("unlock", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue != null && !string.IsNullOrWhiteSpace(normalizedValue.ToString()))
                    {
                        context.AddFailure("This command does not accept a value.");
                    }

                    return;
                }
                
                if (request.Command.Equals("setBrightness", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue == null)
                    {
                        context.AddFailure("Brightness value is required.");
                        return;
                    }

                    if (!int.TryParse(normalizedValue.ToString(), out var brightness))
                    {
                        context.AddFailure("Brightness must be a valid integer.");
                        return;
                    }

                    if (brightness < 10 || brightness > 100)
                    {
                        context.AddFailure("Brightness must be between 10 and 100.");
                    }

                    return;
                }

                if (request.Command.Equals("setPower", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue == null)
                    {
                        context.AddFailure("Power state value is required.");
                        return;
                    }

                    var val = normalizedValue.ToString();

                    if (!Enum.TryParse<PowerState>(val, true, out var parsedPower)
                        || !Enum.IsDefined(parsedPower))
                    {
                        context.AddFailure($"Power state must be one of: {string.Join(", ", Enum.GetNames<PowerState>())}.");
                    }

                    return;
                }

                if (request.Command.Equals("setSpeed", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue == null)
                    {
                        context.AddFailure("Speed value is required.");
                        return;
                    }

                    var val = normalizedValue.ToString();

                    if (!Enum.TryParse<FanSpeed>(val, true, out var parsedSpeed)
                        || !Enum.IsDefined(parsedSpeed))
                    {
                        context.AddFailure($"Fan speed must be one of: {string.Join(", ", Enum.GetNames<FanSpeed>())}.");
                    }

                    return;
                }

                if (request.Command.Equals("setMode", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue == null)
                    {
                        context.AddFailure("Mode value is required.");
                        return;
                    }

                    var val = normalizedValue.ToString();

                    if (!Enum.TryParse<ThermostatMode>(val, true, out var parsedMode)
                        || !Enum.IsDefined(parsedMode))
                    {
                        context.AddFailure($"Thermostat mode must be one of: {string.Join(", ", Enum.GetNames<ThermostatMode>())}.");
                    }

                    return;
                }

                if (request.Command.Equals("setDesiredTemperature", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedValue == null)
                    {
                        context.AddFailure("Temperature value is required.");
                        return;
                    }

                    if (!int.TryParse(normalizedValue.ToString(), out var temp))
                    {
                        context.AddFailure("Temperature must be a valid integer.");
                        return;
                    }

                    if (temp < 60 || temp > 80)
                    {
                        context.AddFailure("Temperature must be between 60 and 80.");
                    }

                    return;
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
