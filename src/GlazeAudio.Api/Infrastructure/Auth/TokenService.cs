using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GlazeAudio.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GlazeAudio.Api.Infrastructure.Auth;

/// <summary>Creates and validates JWT access tokens, and creates refresh tokens.</summary>
public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(_options.SigningKey));

    /// <summary>
    /// A signed JWT carrying the user's id (sub), username (name) and role (role).
    /// The role inside the token is what the API's authorization checks use.
    /// </summary>
    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expires,
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.Username),
                new Claim(AuthClaims.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ])
        });
        return (token, expires);
    }

    public TokenValidationParameters ValidationParameters => new()
    {
        ValidIssuer = _options.Issuer,
        ValidAudience = _options.Audience,
        IssuerSigningKey = SigningKey,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = JwtRegisteredClaimNames.Name,
        RoleClaimType = AuthClaims.Role
    };

    public Task<TokenValidationResult> ValidateAsync(string token) =>
        _handler.ValidateTokenAsync(token, ValidationParameters);

    /// <summary>A random, URL-safe refresh token. Only its SHA-256 hash is stored in the database.</summary>
    public (string Token, DateTime ExpiresAt) CreateRefreshToken() =>
        (Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48)), DateTime.UtcNow.AddDays(_options.RefreshTokenDays));

    public static string Hash(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
