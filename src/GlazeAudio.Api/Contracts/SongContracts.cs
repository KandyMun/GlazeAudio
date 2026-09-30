using System.ComponentModel.DataAnnotations;

namespace GlazeAudio.Api.Contracts;

/// <summary>A song with a summary of its reviews.</summary>
public record SongDto(
    int Id,
    int AlbumId,
    string Title,
    int TrackNumber,
    int DurationSeconds,
    int ReviewCount,
    double? AverageRating);

/// <summary>Body used to create or fully replace a song.</summary>
public record SongRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string? Title,
    [property: Required, Range(1, 99)] int? TrackNumber,
    [property: Required, Range(1, 7200)] int? DurationSeconds);
