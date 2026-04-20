using FluentValidation;
using SmartHome.Api.Controller;

namespace SmartHome.Api.Validation;

/// <summary>
/// Validates SetSimulationSpeedRequest at the HTTP boundary.
/// Ensures the incoming speed is a defined enum value before the request
/// reaches domain code; the registry still has final authority on which
/// speeds are permitted at runtime.
/// </summary>
public sealed class SetSimulationSpeedRequestValidator : AbstractValidator<SetSimulationSpeedRequest>
{
    public SetSimulationSpeedRequestValidator()
    {
        RuleFor(request => request.Speed)
            .IsInEnum()
            .WithMessage("Speed must be one of: Normal, Double, Fast, Ultra.");
    }
}