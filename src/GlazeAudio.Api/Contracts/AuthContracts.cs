using System.ComponentModel.DataAnnotations;

namespace GlazeAudio.Api.Contracts;

/// <summary>Body of POST /api/auth/login. The login can be a username or an email.</summary>
public record LoginRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string? Login,
    [property: Required, StringLength(100, MinimumLength = 1)] string? Password);

/// <summary>Body of POST /api/auth/refresh and POST /api/auth/logout.</summary>
public record RefreshRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string? RefreshToken);

/// <summary>Body of PUT /api/auth/password.</summary>
public record ChangePasswordRequest(
    [property: Required, StringLength(100, MinimumLength = 1)] string? CurrentPassword,
    [property: Required, StringLength(100, MinimumLength = 8)] string? NewPassword);

/// <summary>
/// Returned by register, login and refresh. Send the access token as "Authorization: Bearer &lt;accessToken&gt;";
/// when it expires, exchange the refresh token for a new pair at POST /api/auth/refresh.
/// </summary>
public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User)
{
    public string TokenType => "Bearer";
}
