using System.ComponentModel.DataAnnotations;
using GlazeAudio.Api.Infrastructure;

namespace GlazeAudio.Api.Contracts;

/// <summary>A user account. The password hash is never returned; the email only to the user themself and admins.</summary>
public record UserDto(
    int Id,
    string Username,
    string? Email,
    string Role,
    string? Bio,
    DateTime CreatedAt,
    int ReviewCount) : Resource;

/// <summary>Body used to register a new account (POST /api/auth/register) or create one as an admin.</summary>
public record CreateUserRequest(
    [property: Required, StringLength(50, MinimumLength = 3),
     RegularExpression("^[A-Za-z0-9_.-]+$", ErrorMessage = "Username may contain only letters, digits, '_', '.' and '-'.")]
    string? Username,
    [property: Required, EmailAddress, StringLength(200)] string? Email,
    [property: Required, StringLength(100, MinimumLength = 8)] string? Password,
    [property: StringLength(500)] string? Bio);

/// <summary>Body used to update an account. The password is not changed here; only admins may change the role.</summary>
public record UpdateUserRequest(
    [property: Required, StringLength(50, MinimumLength = 3),
     RegularExpression("^[A-Za-z0-9_.-]+$", ErrorMessage = "Username may contain only letters, digits, '_', '.' and '-'.")]
    string? Username,
    [property: Required, EmailAddress, StringLength(200)] string? Email,
    [property: StringLength(500)] string? Bio,
    [property: RegularExpression("^(User|Admin)$", ErrorMessage = "Role must be 'User' or 'Admin'.")] string? Role = null);

/// <summary>A review as seen from its author's profile, with the song and album it belongs to.</summary>
public record UserReviewDto(
    int Id,
    int AlbumId,
    string AlbumTitle,
    string AlbumArtist,
    int SongId,
    string SongTitle,
    double OverallRating,
    string Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt) : Resource;
