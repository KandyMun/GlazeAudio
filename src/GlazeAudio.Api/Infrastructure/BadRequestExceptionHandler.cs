using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace GlazeAudio.Api.Infrastructure;

/// <summary>
/// Turns requests the server cannot read (malformed JSON, wrong value types, missing body)
/// into a 400 Bad Request problem response instead of a 500.
/// </summary>
public class BadRequestExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not BadHttpRequestException badRequest)
            return false;

        var detail = badRequest.InnerException is JsonException json
            ? $"The JSON payload is invalid: {json.Message}"
            : badRequest.Message.StartsWith("Failed to bind parameter", StringComparison.Ordinal)
                ? badRequest.Message   // e.g. a query value of the wrong type: ?page=abc
                : "The request body is missing or could not be read. Send a JSON body with Content-Type: application/json.";

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request could not be read.",
                Detail = detail
            }
        });
    }
}
