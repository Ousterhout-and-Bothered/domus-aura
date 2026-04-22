using FluentValidation;
using SmartHome.Api.Controller;

namespace SmartHome.Api.Validation;

/// <summary>
/// Validates SetAmbientTemperatureRequest at the HTTP boundary.
/// Enforces a reasonable ambient-temperature range before the request
/// reaches the domain. Wider than the thermostat's desired-temperature
/// clamp (60-80°F) because ambient temperature tracks real-world
/// environmental conditions, which can exceed thermostat setpoints.
/// </summary>
public sealed class SetAmbientTemperatureRequestValidator : AbstractValidator<SetAmbientTemperatureRequest>
{
    public SetAmbientTemperatureRequestValidator()
    {
        RuleFor(request => request.Temperature)
            .Custom((value, context) =>
            {
                var request = context.InstanceToValidate;
                var intValue = request.GetTemperatureValue();

                if (intValue == null)
                {
                    context.AddFailure("Ambient temperature must be a valid number.");
                    return;
                }

                if (intValue.Value < -40 || intValue.Value > 150)
                {
                    context.AddFailure("Ambient temperature must be between -40 and 150 °F.");
                }
            });
    }
}