using GlazeAudio.Api.Contracts;
using GlazeAudio.Api.Data;
using GlazeAudio.Api.Infrastructure;
using GlazeAudio.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlazeAudio.Api.Endpoints;

public static class AlbumEndpoints
{
    public static RouteGroupBuilder MapAlbumEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/albums").WithTags("Albums");

        group.MapGet("/", GetAll)
            .WithName("GetAlbums")
            .WithSummary("List albums (paged, filterable)")
            .WithDescription(
                "Returns one page of albums with their song count, review count and average rating. " +
                "Filter with search, artist, genre, fromYear and toYear. Paging metadata and " +
                "first/prev/next/last links are included in the response.")
            .Produces<PagedResult<AlbumDto>>()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{albumId:int}", GetById)
            .WithName("GetAlbum")
            .WithSummary("Get one album")
            .Produces<AlbumDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{albumId:int}/overview", GetOverview)
            .WithName("GetAlbumOverview")
            .WithSummary("Album overview (album + songs + reviews)")
            .WithDescription(
                "A dashboard-style resource built from three entities: the album, its songs with their ratings, " +
                "rating statistics per aspect across all reviews, the top-rated song and the latest reviews.")
            .Produces<AlbumOverviewDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateAlbum")
            .WithSummary("Create an album")
            .WithDescription("Creates a new album. Returns 201 with a Location header that points to the new album.")
            .WithValidation<AlbumRequest>()
            .Produces<AlbumDto>(StatusCodes.Status201Created);

        group.MapPut("/{albumId:int}", Update)
            .WithName("UpdateAlbum")
            .WithSummary("Update an album")
            .WithDescription("Replaces all editable fields of the album.")
            .WithValidation<AlbumRequest>()
            .Produces<AlbumDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{albumId:int}", Delete)
            .WithName("DeleteAlbum")
            .WithSummary("Delete an album")
            .WithDescription("Deletes the album together with all of its songs and their reviews. Returns 204 with no body.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static IQueryable<AlbumDto> Project(IQueryable<Album> albums) =>
        albums.Select(a => new AlbumDto(
            a.Id, a.Title, a.Artist, a.ReleaseYear, a.Genre, a.CoverUrl,
            a.Songs.Count,
            a.Songs.SelectMany(s => s.Reviews).Count(),
            a.Songs.SelectMany(s => s.Reviews)
                .Average(r => (double?)(r.LyricsRating + r.MelodyRating + r.MoodRating + r.ExpressivenessRating) / 4.0)));

    private static AlbumDto WithLinks(AlbumDto album, HttpRequest request) =>
        album with { Links = ApiLinks.Album(request, album.Id) };

    private static async Task<IResult> GetAll([AsParameters] AlbumQuery query, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        if (Paging.Validate(query.Page, query.PageSize, out var page, out var pageSize) is { } invalid)
            return invalid;

        var albums = db.Albums.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            albums = albums.Where(a => a.Title.ToLower().Contains(term) || a.Artist.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(query.Artist))
        {
            var artist = query.Artist.Trim().ToLower();
            albums = albums.Where(a => a.Artist.ToLower().Contains(artist));
        }
        if (!string.IsNullOrWhiteSpace(query.Genre))
        {
            var genre = query.Genre.Trim().ToLower();
            albums = albums.Where(a => a.Genre.ToLower().Contains(genre));
        }
        if (query.FromYear is { } fromYear) albums = albums.Where(a => a.ReleaseYear >= fromYear);
        if (query.ToYear is { } toYear) albums = albums.Where(a => a.ReleaseYear <= toYear);

        var total = await albums.CountAsync(ct);
        var items = await Project(albums.OrderBy(a => a.Id).Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);

        return Results.Ok(Paging.Create(request, items.Select(a => WithLinks(a, request)).ToList(), page, pageSize, total));
    }

    private static async Task<IResult> GetById(int albumId, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        var album = await Project(db.Albums.Where(a => a.Id == albumId)).FirstOrDefaultAsync(ct);
        return album is null ? ApiProblems.NotFound("Album", albumId) : Results.Ok(WithLinks(album, request));
    }

    private static async Task<IResult> GetOverview(int albumId, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        var album = await db.Albums.AsNoTracking()
            .Include(a => a.Songs).ThenInclude(s => s.Reviews).ThenInclude(r => r.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == albumId, ct);
        if (album is null) return ApiProblems.NotFound("Album", albumId);

        static double Round(double value) => Math.Round(value, 2);

        var reviews = album.Songs.SelectMany(s => s.Reviews.Select(r => (Song: s, Review: r))).ToList();

        var songs = album.Songs
            .OrderBy(s => s.TrackNumber).ThenBy(s => s.Id)
            .Select(s => new OverviewSong(
                s.Id, s.TrackNumber, s.Title, s.DurationSeconds,
                s.Reviews.Count,
                s.Reviews.Count == 0 ? null : Round(s.Reviews.Average(r => r.OverallRating)))
            {
                Links = ApiLinks.Song(request, albumId, s.Id)
            })
            .ToList();

        var stats = new AlbumStats(
            SongCount: album.Songs.Count,
            ReviewCount: reviews.Count,
            TotalDurationSeconds: album.Songs.Sum(s => s.DurationSeconds),
            AverageRating: reviews.Count == 0 ? null : Round(reviews.Average(x => x.Review.OverallRating)),
            AspectAverages: reviews.Count == 0 ? null : new AspectAverages(
                Round(reviews.Average(x => x.Review.LyricsRating)),
                Round(reviews.Average(x => x.Review.MelodyRating)),
                Round(reviews.Average(x => x.Review.MoodRating)),
                Round(reviews.Average(x => x.Review.ExpressivenessRating))));

        var topRated = songs
            .Where(s => s.AverageRating is not null)
            .OrderByDescending(s => s.AverageRating).ThenByDescending(s => s.ReviewCount)
            .FirstOrDefault();

        var latest = reviews
            .OrderByDescending(x => x.Review.CreatedAt).ThenByDescending(x => x.Review.Id)
            .Take(5)
            .Select(x => new OverviewReview(
                x.Review.Id, x.Song.Id, x.Song.Title, x.Review.UserId, x.Review.User.Username,
                x.Review.OverallRating, x.Review.Comment, x.Review.CreatedAt)
            {
                Links = ApiLinks.Review(request, albumId, x.Song.Id, x.Review.Id, x.Review.UserId)
            })
            .ToList();

        var overview = new AlbumOverviewDto(
            new AlbumSummary(album.Id, album.Title, album.Artist, album.ReleaseYear, album.Genre, album.CoverUrl),
            stats, songs, topRated, latest)
        {
            Links = ApiLinks.Overview(request, albumId)
        };
        return Results.Ok(overview);
    }

    private static async Task<IResult> Create(AlbumRequest body, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        var album = new Album
        {
            Title = body.Title!.Trim(),
            Artist = body.Artist!.Trim(),
            ReleaseYear = body.ReleaseYear!.Value,
            Genre = body.Genre!.Trim(),
            CoverUrl = body.CoverUrl
        };
        db.Albums.Add(album);
        await db.SaveChangesAsync(ct);

        var dto = new AlbumDto(album.Id, album.Title, album.Artist, album.ReleaseYear, album.Genre, album.CoverUrl, 0, 0, null);
        return Results.CreatedAtRoute("GetAlbum", new { albumId = album.Id }, WithLinks(dto, request));
    }

    private static async Task<IResult> Update(int albumId, AlbumRequest body, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        var album = await db.Albums.FindAsync([albumId], ct);
        if (album is null) return ApiProblems.NotFound("Album", albumId);

        album.Title = body.Title!.Trim();
        album.Artist = body.Artist!.Trim();
        album.ReleaseYear = body.ReleaseYear!.Value;
        album.Genre = body.Genre!.Trim();
        album.CoverUrl = body.CoverUrl;
        await db.SaveChangesAsync(ct);

        var dto = await Project(db.Albums.Where(a => a.Id == albumId)).FirstAsync(ct);
        return Results.Ok(WithLinks(dto, request));
    }

    private static async Task<IResult> Delete(int albumId, GlazeAudioDbContext db, CancellationToken ct)
    {
        var deleted = await db.Albums.Where(a => a.Id == albumId).ExecuteDeleteAsync(ct);
        return deleted == 0 ? ApiProblems.NotFound("Album", albumId) : Results.NoContent();
    }
}
