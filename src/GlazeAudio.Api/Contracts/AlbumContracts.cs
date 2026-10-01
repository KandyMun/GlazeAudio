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

/// <summary>Body used to create or fully replace an album.</summary>
public record AlbumRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string? Title,
    [property: Required, StringLength(200, MinimumLength = 1)] string? Artist,
    [property: Required, Range(1900, 2100)] int? ReleaseYear,
    [property: Required, StringLength(100, MinimumLength = 1)] string? Genre,
    [property: Url, StringLength(500)] string? CoverUrl);
