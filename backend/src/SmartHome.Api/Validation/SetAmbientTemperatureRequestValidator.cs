using FluentValidation;
using SmartHome.Api.Controllers;

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
            .InclusiveBetween(-40, 150)
            .WithMessage("Ambient temperature must be between -40 and 150 °F.");
    }
}