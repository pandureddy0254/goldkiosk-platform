using FluentValidation;
using GoldKiosk.Contracts.V1.Agent;

namespace GoldKiosk.Kiosk.Api.Validation;

/// <summary>Validates <see cref="AgentItemCheckRequest"/> at the boundary.</summary>
public sealed class AgentItemCheckRequestValidator : AbstractValidator<AgentItemCheckRequest>
{
    /// <summary>Initializes the rules.</summary>
    public AgentItemCheckRequestValidator()
    {
        RuleFor(x => x.Trigger).NotEmpty().MaximumLength(64);
    }
}
