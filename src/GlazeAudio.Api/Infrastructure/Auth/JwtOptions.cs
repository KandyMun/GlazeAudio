namespace GlazeAudio.Api.Infrastructure.Auth;

/// <summary>Settings from the "Jwt" configuration section.</summary>
public class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "GlazeAudio";
    public string Audience { get; set; } = "GlazeAudio";

    /// <summary>
    /// Secret used to sign tokens (HMAC-SHA256), at least 32 characters.
    /// Never committed: set it with user-secrets locally and as an App Service setting (Jwt__SigningKey) in Azure.
    /// </summary>
    public string SigningKey { get; set; } = "";

    /// <summary>How long an access token is valid. Short, because it cannot be revoked.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>How long a refresh token is valid. Refresh tokens are stored in the database and can be revoked.</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
