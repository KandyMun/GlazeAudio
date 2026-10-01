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
            .WithSummary("List reviews of a song (paged, filterable)")
            .WithDescription(
                "Returns one page of the song's reviews, newest first. Filter with author (username), userId, minRating " +
                "and maxRating (overall rating = average of the 4 aspects). 404 if the album or song does not exist, " +
                "or the song is not on that album.")
            .Produces<PagedResult<ReviewDto>>()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{reviewId:int}", GetById)
            .WithName("GetReview")
            .WithSummary("Get one review")
            .Produces<ReviewDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateReview")
            .WithSummary("Review a song")
            .WithDescription(
                "Each aspect (lyrics, melody, mood, expressiveness) is rated 0–5. The author is given as userId " +
                "(until authentication is added). 422 if a rating is out of range or the user does not exist; " +
                "409 if this user has already reviewed the song.")
            .WithValidation<CreateReviewRequest>()
            .Produces<ReviewDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{reviewId:int}", Update)
            .WithName("UpdateReview")
            .WithSummary("Edit a review")
            .WithDescription("Replaces the ratings and comment. The author cannot be changed.")
            .WithValidation<UpdateReviewRequest>()
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

    /// <summary>Maps a review (with its User loaded) to the response shape, including hypermedia links.</summary>
    private static ReviewDto ToDto(Review r, int albumId, HttpRequest request) => new(
        r.Id, r.SongId, r.UserId, r.User.Username,
        r.LyricsRating, r.MelodyRating, r.MoodRating, r.ExpressivenessRating,
        r.OverallRating, r.Comment, r.CreatedAt, r.UpdatedAt)
    {
        Links = ApiLinks.Review(request, albumId, r.SongId, r.Id, r.UserId)
    };

    /// <summary>Checks the album → song hierarchy. Returns a 404 result, or null if the path is valid.</summary>
    private static async Task<IResult?> CheckParents(int albumId, int songId, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (!await db.Albums.AnyAsync(a => a.Id == albumId, ct))
            return ApiProblems.NotFound("Album", albumId);
        if (!await db.Songs.AnyAsync(s => s.Id == songId && s.AlbumId == albumId, ct))
            return ApiProblems.NotFound("Song", songId);
        return null;
    }

    private static async Task<IResult> GetAll(int albumId, int songId, [AsParameters] ReviewQuery query, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        if (Paging.Validate(query.Page, query.PageSize, out var page, out var pageSize) is { } invalid)
            return invalid;
        if (query.MinRating is < 0 or > 5 || query.MaxRating is < 0 or > 5)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["rating"] = ["minRating and maxRating must be between 0 and 5."] },
                statusCode: StatusCodes.Status400BadRequest, title: "Invalid query parameters.");
        }
        if (await CheckParents(albumId, songId, db, ct) is { } notFound) return notFound;

        var reviews = db.Reviews.AsNoTracking().Include(r => r.User).Where(r => r.SongId == songId);

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            var author = query.Author.Trim().ToLower();
            reviews = reviews.Where(r => r.User.Username.ToLower() == author);
        }
        if (query.UserId is { } userId) reviews = reviews.Where(r => r.UserId == userId);

        // Overall rating = sum of the 4 aspects / 4, so compare the sum against rating * 4 (works in SQL).
        if (query.MinRating is { } minRating)
        {
            var minSum = minRating * 4;
            reviews = reviews.Where(r => r.LyricsRating + r.MelodyRating + r.MoodRating + r.ExpressivenessRating >= minSum);
        }
        if (query.MaxRating is { } maxRating)
        {
            var maxSum = maxRating * 4;
            reviews = reviews.Where(r => r.LyricsRating + r.MelodyRating + r.MoodRating + r.ExpressivenessRating <= maxSum);
        }

        var total = await reviews.CountAsync(ct);
        var items = await reviews
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(Paging.Create(request, items.Select(r => ToDto(r, albumId, request)).ToList(), page, pageSize, total));
    }

    private static async Task<IResult> GetById(int albumId, int songId, int reviewId, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        if (await CheckParents(albumId, songId, db, ct) is { } notFound) return notFound;

        var review = await db.Reviews.AsNoTracking().Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.SongId == songId, ct);
        return review is null ? ApiProblems.NotFound("Review", reviewId) : Results.Ok(ToDto(review, albumId, request));
    }

    private static async Task<IResult> Create(int albumId, int songId, CreateReviewRequest body, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        if (await CheckParents(albumId, songId, db, ct) is { } notFound) return notFound;

        var author = await db.Users.FindAsync([body.UserId!.Value], ct);
        if (author is null)
            return ApiProblems.InvalidField("userId", $"User with id {body.UserId} does not exist.");

        if (await db.Reviews.AnyAsync(r => r.SongId == songId && r.UserId == author.Id, ct))
        {
            return ApiProblems.Conflict("Review already exists.",
                $"User '{author.Username}' has already reviewed this song. Edit the existing review instead.");
        }

        var review = new Review
        {
            SongId = songId,
            User = author,
            LyricsRating = body.LyricsRating!.Value,
            MelodyRating = body.MelodyRating!.Value,
            MoodRating = body.MoodRating!.Value,
            ExpressivenessRating = body.ExpressivenessRating!.Value,
            Comment = body.Comment!.Trim()
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);

        return Results.CreatedAtRoute("GetReview", new { albumId, songId, reviewId = review.Id }, ToDto(review, albumId, request));
    }

    private static async Task<IResult> Update(int albumId, int songId, int reviewId, UpdateReviewRequest body, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        if (await CheckParents(albumId, songId, db, ct) is { } notFound) return notFound;

        var review = await db.Reviews.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == reviewId && r.SongId == songId, ct);
        if (review is null) return ApiProblems.NotFound("Review", reviewId);

        review.LyricsRating = body.LyricsRating!.Value;
        review.MelodyRating = body.MelodyRating!.Value;
        review.MoodRating = body.MoodRating!.Value;
        review.ExpressivenessRating = body.ExpressivenessRating!.Value;
        review.Comment = body.Comment!.Trim();
        review.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(ToDto(review, albumId, request));
    }

    private static async Task<IResult> Delete(int albumId, int songId, int reviewId, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (await CheckParents(albumId, songId, db, ct) is { } notFound) return notFound;

        var deleted = await db.Reviews.Where(r => r.Id == reviewId && r.SongId == songId).ExecuteDeleteAsync(ct);
        return deleted == 0 ? ApiProblems.NotFound("Review", reviewId) : Results.NoContent();
    }
}
