namespace GlazeAudio.Api.Infrastructure;

public static class ApiProblems
{
    public static IResult NotFound(string resource, int id) =>
        Results.Problem(
            title: $"{resource} not found.",
            detail: $"{resource} with id {id} does not exist.",
            statusCode: StatusCodes.Status404NotFound);
}
