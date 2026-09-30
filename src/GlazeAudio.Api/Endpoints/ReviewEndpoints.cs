using GlazeAudio.Api.Contracts;
using GlazeAudio.Api.Data;
using GlazeAudio.Api.Infrastructure;
using GlazeAudio.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlazeAudio.Api.Endpoints;

public static class ReviewEndpoints
{
    public static RouteGroupBuilder MapReviewEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/albums/{albumId:int}/songs/{songId:int}/reviews").WithTags("Reviews");

        group.MapGet("/", GetAll)
            .WithName("GetReviews")
            .WithSummary("List reviews of a song")
            .WithDescription("Returns the song's reviews, newest first. 404 if the album or song does not exist, or the song is not on that album.")
            .Produces<List<ReviewDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{reviewId:int}", GetById)
            .WithName("GetReview")
            .WithSummary("Get one review")
            .Produces<ReviewDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateReview")
            .WithSummary("Review a song")
            .WithDescription("Each aspect (lyrics, melody, mood, expressiveness) is rated 0–5. A rating outside that range returns 422.")
            .WithValidation<ReviewRequest>()
            .Produces<ReviewDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{reviewId:int}", Update)
            .WithName("UpdateReview")
            .WithSummary("Edit a review")
            .WithValidation<ReviewRequest>()
            .Produces<ReviewDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{reviewId:int}", Delete)
            .WithName("DeleteReview")
            .WithSummary("Delete a review")
            .WithDescription("Used by administrators to remove harmful reviews. Returns 204 with no body.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static ReviewDto ToDto(Review r) => new(
        r.Id, r.SongId, r.AuthorName,
        r.LyricsRating, r.MelodyRating, r.MoodRating, r.ExpressivenessRating,
        r.OverallRating, r.Comment, r.CreatedAt, r.UpdatedAt);

    /// <summary>Checks the album → song hierarchy. Returns a 404 result, or null if the path is valid.</summary>
    private static async Task<IResult?> CheckParents(int albumId, int songId, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (!await db.Albums.AnyAsync(a => a.Id == albumId, ct))
            return ApiProblems.NotFound("Album", albumId);
        if (!await db.Songs.AnyAsync(s => s.Id == songId && s.AlbumId == albumId, ct))
            return ApiProblems.NotFound("Song", songId);
        return null;
    }

    private static async Task<IResult> GetAll(int albumId, int songId, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (await CheckParents(albumId, songId, db, ct) is { } notFound) return notFound;

        var reviews = await db.Reviews.AsNoTracking()
            .Where(r => r.SongId == songId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .ToListAsync(ct);
        return Results.Ok(reviews.Select(ToDto).ToList());
    }

    private static async Task<IResult> GetById(int albumId, int songId, int reviewId, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (await CheckParents(albumId, songId, db, ct) is { } notFound) return notFound;

        var review = await db.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reviewId && r.SongId == songId, ct);
        return review is null ? ApiProblems.NotFound("Review", reviewId) : Results.Ok(ToDto(review));
    }

    private static async Task<IResult> Create(int albumId, int songId, ReviewRequest request, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (await CheckParents(albumId, songId, db, ct) is { } notFound) return notFound;

        var review = new Review
        {
            SongId = songId,
            AuthorName = request.AuthorName!.Trim(),
            LyricsRating = request.LyricsRating!.Value,
            MelodyRating = request.MelodyRating!.Value,
            MoodRating = request.MoodRating!.Value,
            ExpressivenessRating = request.ExpressivenessRating!.Value,
            Comment = request.Comment!.Trim()
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);

        return Results.CreatedAtRoute("GetReview", new { albumId, songId, reviewId = review.Id }, ToDto(review));
    }

    private static async Task<IResult> Update(int albumId, int songId, int reviewId, ReviewRequest request, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (await CheckParents(albumId, songId, db, ct) is { } notFound) return notFound;

        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.SongId == songId, ct);
        if (review is null) return ApiProblems.NotFound("Review", reviewId);

        review.AuthorName = request.AuthorName!.Trim();
        review.LyricsRating = request.LyricsRating!.Value;
        review.MelodyRating = request.MelodyRating!.Value;
        review.MoodRating = request.MoodRating!.Value;
        review.ExpressivenessRating = request.ExpressivenessRating!.Value;
        review.Comment = request.Comment!.Trim();
        review.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(ToDto(review));
    }

    private static async Task<IResult> Delete(int albumId, int songId, int reviewId, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (await CheckParents(albumId, songId, db, ct) is { } notFound) return notFound;

        var deleted = await db.Reviews.Where(r => r.Id == reviewId && r.SongId == songId).ExecuteDeleteAsync(ct);
        return deleted == 0 ? ApiProblems.NotFound("Review", reviewId) : Results.NoContent();
    }
}
