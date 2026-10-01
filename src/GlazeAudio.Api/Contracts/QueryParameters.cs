using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace GlazeAudio.Api.Contracts;

/// <summary>Query string for GET /api/albums – paging + filters.</summary>
public class AlbumQuery
{
    [Description("Page number, starting at 1. Default 1.")]
    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [Description("Items per page, 1–50. Default 10.")]
    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; set; }

    [Description("Text to find in the album title or artist (case-insensitive).")]
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [Description("Only albums whose artist contains this text.")]
    [FromQuery(Name = "artist")]
    public string? Artist { get; set; }

    [Description("Only albums whose genre contains this text, e.g. 'rock'.")]
    [FromQuery(Name = "genre")]
    public string? Genre { get; set; }

    [Description("Only albums released in or after this year.")]
    [FromQuery(Name = "fromYear")]
    public int? FromYear { get; set; }

    [Description("Only albums released in or before this year.")]
    [FromQuery(Name = "toYear")]
    public int? ToYear { get; set; }
}

/// <summary>Query string for GET /api/albums/{albumId}/songs – paging + filters.</summary>
public class SongQuery
{
    [Description("Page number, starting at 1. Default 1.")]
    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [Description("Items per page, 1–50. Default 10.")]
    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; set; }

    [Description("Text to find in the song title (case-insensitive).")]
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [Description("Only songs at least this long (seconds).")]
    [FromQuery(Name = "minDuration")]
    public int? MinDuration { get; set; }

    [Description("Only songs at most this long (seconds).")]
    [FromQuery(Name = "maxDuration")]
    public int? MaxDuration { get; set; }
}

/// <summary>Query string for GET .../reviews – paging + filters.</summary>
public class ReviewQuery
{
    [Description("Page number, starting at 1. Default 1.")]
    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [Description("Items per page, 1–50. Default 10.")]
    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; set; }

    [Description("Only reviews by this author's username (exact match, case-insensitive).")]
    [FromQuery(Name = "author")]
    public string? Author { get; set; }

    [Description("Only reviews written by this user id.")]
    [FromQuery(Name = "userId")]
    public int? UserId { get; set; }

    [Description("Only reviews whose overall rating (average of the 4 aspects) is at least this value, 0–5.")]
    [FromQuery(Name = "minRating")]
    public double? MinRating { get; set; }

    [Description("Only reviews whose overall rating is at most this value, 0–5.")]
    [FromQuery(Name = "maxRating")]
    public double? MaxRating { get; set; }
}

/// <summary>Query string for GET /api/users – paging + filters.</summary>
public class UserQuery
{
    [Description("Page number, starting at 1. Default 1.")]
    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [Description("Items per page, 1–50. Default 10.")]
    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; set; }

    [Description("Text to find in the username or email (case-insensitive).")]
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [Description("Only users with this role: User or Admin.")]
    [FromQuery(Name = "role")]
    public string? Role { get; set; }
}

/// <summary>Query string for GET /api/users/{userId}/reviews – paging + filters.</summary>
public class UserReviewQuery
{
    [Description("Page number, starting at 1. Default 1.")]
    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [Description("Items per page, 1–50. Default 10.")]
    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; set; }

    [Description("Only reviews whose overall rating is at least this value, 0–5.")]
    [FromQuery(Name = "minRating")]
    public double? MinRating { get; set; }
}
