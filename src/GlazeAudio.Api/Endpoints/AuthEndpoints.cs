using System.Security.Claims;
using GlazeAudio.Api.Contracts;
using GlazeAudio.Api.Data;
using GlazeAudio.Api.Infrastructure;
using GlazeAudio.Api.Infrastructure.Auth;
using GlazeAudio.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GlazeAudio.Api.Endpoints;

/// <summary>
/// Registration, login and token management.
/// Login returns a short-lived JWT access token (with the user's role inside) and a long-lived refresh token.
/// </summary>
public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Create an account and log in")
            .WithDescription("Creates an account with the User role and returns tokens, so the client is logged in right away. 409 if the username or email is taken.")
            .WithValidation<CreateUserRequest>()
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Log in with username (or email) and password")
            .WithDescription("Returns an access token (valid 15 minutes, contains the user's id, name and role) and a refresh token (valid 7 days). 401 for a wrong login or password.")
            .WithValidation<LoginRequest>()
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", Refresh)
            .WithName("RefreshToken")
            .WithSummary("Get a new access token")
            .WithDescription("Exchanges a valid refresh token for a new access token and a new refresh token (the old one stops working). 401 if the refresh token is invalid, expired or revoked.")
            .WithValidation<RefreshRequest>()
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithSummary("Log out (revoke a refresh token)")
            .WithDescription("Revokes the refresh token so it can no longer be used. Returns 204 even if the token was already invalid.")
            .WithValidation<RefreshRequest>()
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", Me)
            .WithName("GetCurrentUser")
            .WithSummary("The logged-in user")
            .RequireAuthorization()
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/password", ChangePassword)
            .WithName("ChangePassword")
            .WithSummary("Change your password")
            .WithDescription("Logs out all other sessions (revokes every refresh token). 422 if the current password is wrong.")
            .RequireAuthorization()
            .WithValidation<ChangePasswordRequest>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    /// <summary>Issues an access token + a stored refresh token for the user.</summary>
    private static async Task<AuthResponse> IssueTokens(User user, GlazeAudioDbContext db, TokenService tokens, HttpRequest request, CancellationToken ct)
    {
        var (accessToken, accessExpires) = tokens.CreateAccessToken(user);
        var (refreshToken, refreshExpires) = tokens.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenService.Hash(refreshToken),
            ExpiresAt = refreshExpires
        });
        await db.SaveChangesAsync(ct);

        var reviewCount = await db.Reviews.CountAsync(r => r.UserId == user.Id, ct);
        var dto = new UserDto(user.Id, user.Username, user.Email, user.Role, user.Bio, user.CreatedAt, reviewCount)
        {
            Links = ApiLinks.User(request, user.Id)
        };
        return new AuthResponse(accessToken, accessExpires, refreshToken, refreshExpires, dto);
    }

    private static async Task<IResult> Register(CreateUserRequest body, GlazeAudioDbContext db, IPasswordHasher<User> hasher,
        TokenService tokens, HttpRequest request, CancellationToken ct)
    {
        var username = body.Username!.Trim();
        var email = body.Email!.Trim();
        if (await UserEndpoints.CheckUnique(db, username, email, null, ct) is { } conflict) return conflict;

        var user = new User
        {
            Username = username,
            Email = email,
            Bio = string.IsNullOrWhiteSpace(body.Bio) ? null : body.Bio.Trim(),
            Role = UserRoles.User
        };
        user.PasswordHash = hasher.HashPassword(user, body.Password!);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var response = await IssueTokens(user, db, tokens, request, ct);
        return Results.CreatedAtRoute("GetUser", new { userId = user.Id }, response);
    }

    private static async Task<IResult> Login(LoginRequest body, GlazeAudioDbContext db, IPasswordHasher<User> hasher,
        TokenService tokens, HttpRequest request, CancellationToken ct)
    {
        var login = body.Login!.Trim().ToLower();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == login || u.Email.ToLower() == login, ct);

        var result = user is null || string.IsNullOrEmpty(user.PasswordHash)
            ? PasswordVerificationResult.Failed
            : hasher.VerifyHashedPassword(user, user.PasswordHash, body.Password!);

        // Same answer for "no such user" and "wrong password", so logins can't be probed.
        if (result == PasswordVerificationResult.Failed)
            return ApiProblems.Unauthorized("Invalid login or password.", "Check your username/email and password.");

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user!.PasswordHash = hasher.HashPassword(user, body.Password!);

        return Results.Ok(await IssueTokens(user!, db, tokens, request, ct));
    }

    private static async Task<IResult> Refresh(RefreshRequest body, GlazeAudioDbContext db, TokenService tokens,
        HttpRequest request, CancellationToken ct)
    {
        var hash = TokenService.Hash(body.RefreshToken!);
        var stored = await db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
            return ApiProblems.Unauthorized("Invalid refresh token.", "Log in again.");

        if (stored.RevokedAt is not null)
        {
            // A revoked token being used again means it may have been stolen: log the user out everywhere.
            await db.RefreshTokens.Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
            return ApiProblems.Unauthorized("Refresh token was already used.", "For safety all sessions were logged out. Log in again.");
        }

        if (stored.ExpiresAt <= DateTime.UtcNow)
            return ApiProblems.Unauthorized("Refresh token expired.", "Log in again.");

        // Rotate: the old refresh token is revoked and replaced by a new one.
        var response = await IssueTokens(stored.User, db, tokens, request, ct);
        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByTokenHash = TokenService.Hash(response.RefreshToken);
        await db.SaveChangesAsync(ct);

        return Results.Ok(response);
    }

    private static async Task<IResult> Logout(RefreshRequest body, GlazeAudioDbContext db, CancellationToken ct)
    {
        var hash = TokenService.Hash(body.RefreshToken!);
        await db.RefreshTokens.Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Me(ClaimsPrincipal principal, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var user = await UserEndpoints.Project(db.Users.Where(u => u.Id == userId), showEmail: true).FirstOrDefaultAsync(ct);
        return user is null
            ? ApiProblems.Unauthorized("Account no longer exists.", "Log in again.")
            : Results.Ok(user with { Links = ApiLinks.User(request, user.Id) });
    }

    private static async Task<IResult> ChangePassword(ChangePasswordRequest body, ClaimsPrincipal principal, GlazeAudioDbContext db,
        IPasswordHasher<User> hasher, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([principal.GetUserId()], ct);
        if (user is null)
            return ApiProblems.Unauthorized("Account no longer exists.", "Log in again.");

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, body.CurrentPassword!) == PasswordVerificationResult.Failed)
            return ApiProblems.InvalidField("currentPassword", "The current password is wrong.");

        user.PasswordHash = hasher.HashPassword(user, body.NewPassword!);
        await db.SaveChangesAsync(ct);
        await db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
        return Results.NoContent();
    }
}
