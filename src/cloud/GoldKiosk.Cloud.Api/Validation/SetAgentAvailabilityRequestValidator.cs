using FluentValidation;
using GoldKiosk.Contracts.V1.Cloud.LiveAgent;

namespace GoldKiosk.Cloud.Api.Validation;

/// <summary>
/// Validates <see cref="SetAgentAvailabilityRequest"/> — the agent count stays sane.
/// </summary>
public sealed class SetAgentAvailabilityRequestValidator : AbstractValidator<SetAgentAvailabilityRequest>
{
    /// <summary>Initializes a new instance of the <see cref="SetAgentAvailabilityRequestValidator"/> class.</summary>
    public SetAgentAvailabilityRequestValidator()
    {
        RuleFor(x => x.AgentsOnline).InclusiveBetween(0, 10_000).When(x => x.AgentsOnline is not null);
    }
}
