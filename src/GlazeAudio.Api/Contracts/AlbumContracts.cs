using System.ComponentModel.DataAnnotations;
using GlazeAudio.Api.Infrastructure;

namespace GlazeAudio.Api.Contracts;

/// <summary>An album with a summary of its songs and reviews.</summary>
public record AlbumDto(
    int Id,
    string Title,
    string Artist,
    int ReleaseYear,
    string Genre,
    string? CoverUrl,
    int SongCount,
    int ReviewCount,
    double? AverageRating) : Resource;

/// <summary>
/// Body used to create or fully replace an album.
/// CoverUrl is either an absolute http(s) address or a path on the website, e.g. /covers/cobalt-drip.jpg
/// (the frontend ships a set of cover images under /covers).
/// </summary>
public record AlbumRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string? Title,
    [property: Required, StringLength(200, MinimumLength = 1)] string? Artist,
    [property: Required, Range(1900, 2100)] int? ReleaseYear,
    [property: Required, StringLength(100, MinimumLength = 1)] string? Genre,
    [property: StringLength(500),
     RegularExpression(@"^(https?://[^\s]+|/[^\s]*)$", ErrorMessage = "Cover URL must be an http(s) address or a path starting with '/'.")]
    string? CoverUrl);
