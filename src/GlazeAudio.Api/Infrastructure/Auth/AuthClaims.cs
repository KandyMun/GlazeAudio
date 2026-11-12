using System.Security.Claims;
using GlazeAudio.Api.Models;
using Microsoft.IdentityModel.JsonWebTokens;

namespace GlazeAudio.Api.Infrastructure.Auth;

public static class AuthClaims
{
    /// <summary>Claim type of the role inside the JWT.</summary>
    public const string Role = "role";

    public const string AdminPolicy = "Admin";

    /// <summary>The logged-in user's id from the token's "sub" claim.</summary>
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? throw new InvalidOperationException("The token has no 'sub' claim."));

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole(UserRoles.Admin);

    /// <summary>True for the owner of a resource or an admin.</summary>
    public static bool IsSelfOrAdmin(this ClaimsPrincipal principal, int ownerUserId) =>
        principal.Identity?.IsAuthenticated == true && (principal.IsAdmin() || principal.GetUserId() == ownerUserId);
}
