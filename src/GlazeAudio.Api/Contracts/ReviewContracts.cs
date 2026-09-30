using System.ComponentModel.DataAnnotations;

namespace GlazeAudio.Api.Contracts;

/// <summary>A song review. Each aspect is rated 0–5; OverallRating is their average.</summary>
public record ReviewDto(
    int Id,
    int SongId,
    string AuthorName,
    int LyricsRating,
    int MelodyRating,
    int MoodRating,
    int ExpressivenessRating,
    double OverallRating,
    string Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Body used to create or fully replace a review.</summary>
public record ReviewRequest(
    [property: Required, StringLength(100, MinimumLength = 1)] string? AuthorName,
    [property: Required, Range(0, 5)] int? LyricsRating,
    [property: Required, Range(0, 5)] int? MelodyRating,
    [property: Required, Range(0, 5)] int? MoodRating,
    [property: Required, Range(0, 5)] int? ExpressivenessRating,
    [property: Required, StringLength(2000, MinimumLength = 1)] string? Comment);
