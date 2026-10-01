using System.ComponentModel.DataAnnotations;
using GlazeAudio.Api.Infrastructure;

namespace GlazeAudio.Api.Contracts;

/// <summary>A song review. Each aspect is rated 0–5; OverallRating is their average.</summary>
public record ReviewDto(
    int Id,
    int SongId,
    int UserId,
    string Username,
    int LyricsRating,
    int MelodyRating,
    int MoodRating,
    int ExpressivenessRating,
    double OverallRating,
    string Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt) : Resource;

/// <summary>
/// Body used to create a review. Until authentication is added the author is given as userId;
/// later it will come from the logged-in user's token.
/// </summary>
public record CreateReviewRequest(
    [property: Required, Range(1, int.MaxValue)] int? UserId,
    [property: Required, Range(0, 5)] int? LyricsRating,
    [property: Required, Range(0, 5)] int? MelodyRating,
    [property: Required, Range(0, 5)] int? MoodRating,
    [property: Required, Range(0, 5)] int? ExpressivenessRating,
    [property: Required, StringLength(2000, MinimumLength = 1)] string? Comment);

/// <summary>Body used to edit a review. The author cannot be changed.</summary>
public record UpdateReviewRequest(
    [property: Required, Range(0, 5)] int? LyricsRating,
    [property: Required, Range(0, 5)] int? MelodyRating,
    [property: Required, Range(0, 5)] int? MoodRating,
    [property: Required, Range(0, 5)] int? ExpressivenessRating,
    [property: Required, StringLength(2000, MinimumLength = 1)] string? Comment);
