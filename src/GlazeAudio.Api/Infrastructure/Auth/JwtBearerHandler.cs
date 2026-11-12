using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GlazeAudio.Api.Infrastructure.Auth;

/// <summary>
/// Bearer authentication: reads "Authorization: Bearer &lt;jwt&gt;", validates the signature, issuer, audience
/// and expiry, and turns the token's claims (sub, name, role) into the request's user. ASP.NET Core's
/// authorization (RequireAuthorization, roles, policies) then works on that user as usual.
/// Failures are returned as problem+json: 401 (no or invalid token) and 403 (wrong role / not the owner).
/// </summary>
public class JwtBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TokenService tokens,
    IProblemDetailsService problems) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Bearer";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = header["Bearer ".Length..].Trim();
        var result = await tokens.ValidateAsync(token);
        if (!result.IsValid)
            return AuthenticateResult.Fail(result.Exception?.Message ?? "Invalid token.");

        var principal = new System.Security.Claims.ClaimsPrincipal(result.ClaimsIdentity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var failure = (await HandleAuthenticateOnceSafeAsync()).Failure;
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = failure is null ? "Bearer" : "Bearer error=\"invalid_token\"";

        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = failure is null ? "Authentication required." : "Invalid or expired token.",
                Detail = failure is null
                    ? "Log in and send the access token as 'Authorization: Bearer <token>'."
                    : "The access token is invalid or has expired. Refresh it with POST /api/auth/refresh."
            }
        });
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden.",
                Detail = "Your role does not allow this action."
            }
        });
    }
}
