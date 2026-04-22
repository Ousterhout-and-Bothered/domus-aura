using FluentValidation;
using SmartHome.Api.Controller;
using SmartHome.Domain.Simulation;

namespace SmartHome.Api.Validation;

/// <summary>
/// Validates SetSimulationSpeedRequest at the HTTP boundary.
/// Ensures the incoming speed is permitted by the simulation speed registry
/// before the request reaches domain code.
/// </summary>
public sealed class SetSimulationSpeedRequestValidator : AbstractValidator<SetSimulationSpeedRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetSimulationSpeedRequestValidator"/> class.
    /// </summary>
    /// <param name="registry">The registry containing permitted simulation speeds.</param>
    public SetSimulationSpeedRequestValidator(ISimulationSpeedRegistry registry)
    {
        RuleFor(request => request.SpeedMultiplier)
            .Custom((value, context) =>
            {
                var allowedValues = string.Join(", ", registry.AllowedSpeeds.Select(s => (int)s).Order());
                var request = context.InstanceToValidate;
                var intValue = request.GetSpeedValue();

                if (intValue == null)
                {
                    context.AddFailure($"Speed must be a number (one of: {allowedValues}).");
                    return;
                }

                if (!registry.IsAllowed((SimulationSpeed)intValue.Value))
                {
                    context.AddFailure($"Speed must be one of: {allowedValues}.");
                }
            });
    }
}