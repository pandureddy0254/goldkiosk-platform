using FluentValidation;
using FluentValidation.Results;

namespace GoldKiosk.Kiosk.Api.Validation;

/// <summary>
/// Shared endpoint filter running the registered FluentValidation validator for the
/// endpoint's body argument of type <typeparamref name="T"/> before the handler executes.
/// Invalid input returns a 400 validation ProblemDetails — external input never reaches
/// the flow unvalidated (security standard §Input).
/// </summary>
/// <typeparam name="T">The request body type to validate.</typeparam>
public sealed class ValidationFilter<T> : IEndpointFilter
    where T : class
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IValidator<T>? validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is not null)
        {
            T? argument = context.Arguments.OfType<T>().FirstOrDefault();
            if (argument is null)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["body"] = ["A request body is required."],
                });
            }

            ValidationResult result = await validator.ValidateAsync(
                argument, context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                return TypedResults.ValidationProblem(result.ToDictionary());
            }
        }

        return await next(context);
    }
}
