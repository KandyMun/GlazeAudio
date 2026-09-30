using System.ComponentModel.DataAnnotations;

namespace GlazeAudio.Api.Infrastructure;

/// <summary>
/// Validates the request body (DataAnnotations) before the handler runs.
/// Well-formed JSON that breaks a rule → 422 Unprocessable Entity with a list of errors.
/// (Malformed JSON never reaches this filter – it is rejected with 400 by <see cref="BadRequestExceptionHandler"/>.)
/// </summary>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var body = context.Arguments.OfType<T>().FirstOrDefault();
        if (body is null)
        {
            return TypedResults.Problem(
                title: "Request body is missing.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(body, new ValidationContext(body), results, validateAllProperties: true))
        {
            var errors = results
                .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty), (r, member) => (member, r.ErrorMessage ?? "Invalid value."))
                .GroupBy(e => JsonName(e.member))
                .ToDictionary(g => g.Key, g => g.Select(e => e.Item2).ToArray());

            return Results.ValidationProblem(
                errors,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "One or more validation errors occurred.",
                type: "https://tools.ietf.org/html/rfc4918#section-11.2");
        }

        return await next(context);
    }

    private static string JsonName(string member) =>
        string.IsNullOrEmpty(member) ? member : char.ToLowerInvariant(member[0]) + member[1..];
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class =>
        builder.AddEndpointFilter<ValidationFilter<T>>()
               .ProducesProblem(StatusCodes.Status400BadRequest)
               .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
}
