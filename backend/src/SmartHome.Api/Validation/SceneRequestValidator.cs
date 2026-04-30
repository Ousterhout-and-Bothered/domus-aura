using FluentValidation;
using SmartHome.Api.Contracts.Scenes;

namespace SmartHome.Api.Validation;

/// <summary>
/// Validates <see cref="SceneRequest"/> at the HTTP boundary.
/// Enforces basic structural requirements; domain-level invariants
/// (empty actions, invalid targeting, invalid operations) are enforced
/// by the domain entities and surfaced via the global exception handler.
/// </summary>
public sealed class SceneRequestValidator : AbstractValidator<SceneRequest>
{
    public SceneRequestValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(r => r.Actions)
            .NotNull()
            .NotEmpty()
            .WithMessage("A scene must contain at least one action.");

        RuleForEach(r => r.Actions).ChildRules(action =>
        {
            action.RuleFor(a => a.Operation)
                .NotEmpty()
                .MaximumLength(50);

            action.RuleFor(a => a)
                .Must(a => a.DeviceId.HasValue ^ a.DeviceType.HasValue)
                .WithMessage("Each action must target exactly one of deviceId or deviceType.");
        });
    }
}