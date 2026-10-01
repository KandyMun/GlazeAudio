using GlazeAudio.Api.Infrastructure;

namespace GlazeAudio.Api.Contracts;

/// <summary>
/// Album "dashboard" assembled from three entities: the album, its songs and their reviews.
/// </summary>
public record AlbumOverviewDto(
    AlbumSummary Album,
    AlbumStats Stats,
    IReadOnlyList<OverviewSong> Songs,
    OverviewSong? TopRatedSong,
    IReadOnlyList<OverviewReview> LatestReviews) : Resource;

public record AlbumSummary(int Id, string Title, string Artist, int ReleaseYear, string Genre, string? CoverUrl);

/// <summary>Totals and averages across all reviews of all songs on the album.</summary>
public record AlbumStats(
    int SongCount,
    int ReviewCount,
    int TotalDurationSeconds,
    double? AverageRating,
    AspectAverages? AspectAverages);

/// <summary>Average rating per review aspect (0–5).</summary>
public record AspectAverages(double Lyrics, double Melody, double Mood, double Expressiveness);

public record OverviewSong(
    int Id,
    int TrackNumber,
    string Title,
    int DurationSeconds,
    int ReviewCount,
    double? AverageRating) : Resource;

public record OverviewReview(
    int Id,
    int SongId,
    string SongTitle,
    int UserId,
    string Username,
    double OverallRating,
    string Comment,
    DateTime CreatedAt) : Resource;
