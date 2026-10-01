namespace GlazeAudio.Api.Infrastructure;

public static class ApiProblems
{
    public static IResult NotFound(string resource, int id) =>
        Results.Problem(
            title: $"{resource} not found.",
            detail: $"{resource} with id {id} does not exist.",
            statusCode: StatusCodes.Status404NotFound);

    public static IResult Conflict(string title, string detail) =>
        Results.Problem(title: title, detail: detail, statusCode: StatusCodes.Status409Conflict);

    /// <summary>422 for one field, in the same shape as the validation filter's errors.</summary>
    public static IResult InvalidField(string field, string message) =>
        Results.ValidationProblem(
            new Dictionary<string, string[]> { [field] = [message] },
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "One or more validation errors occurred.",
            type: "https://tools.ietf.org/html/rfc4918#section-11.2");
}
