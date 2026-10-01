using System.ComponentModel.DataAnnotations;
using GlazeAudio.Api.Infrastructure;

namespace GlazeAudio.Api.Contracts;

/// <summary>A user account. The password hash is never returned.</summary>
public record UserDto(
    int Id,
    string Username,
    string Email,
    string Role,
    string? Bio,
    DateTime CreatedAt,
    int ReviewCount) : Resource;

/// <summary>Body used to register a new account.</summary>
public record CreateUserRequest(
    [property: Required, StringLength(50, MinimumLength = 3),
     RegularExpression("^[A-Za-z0-9_.-]+$", ErrorMessage = "Username may contain only letters, digits, '_', '.' and '-'.")]
    string? Username,
    [property: Required, EmailAddress, StringLength(200)] string? Email,
    [property: Required, StringLength(100, MinimumLength = 8)] string? Password,
    [property: StringLength(500)] string? Bio);

/// <summary>Body used to update an account. The password is not changed here.</summary>
public record UpdateUserRequest(
    [property: Required, StringLength(50, MinimumLength = 3),
     RegularExpression("^[A-Za-z0-9_.-]+$", ErrorMessage = "Username may contain only letters, digits, '_', '.' and '-'.")]
    string? Username,
    [property: Required, EmailAddress, StringLength(200)] string? Email,
    [property: StringLength(500)] string? Bio,
    [property: Required, AllowedValues("User", "Admin", ErrorMessage = "Role must be 'User' or 'Admin'.")] string? Role);

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
