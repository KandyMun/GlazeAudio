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
            .WithSummary("List all albums")
            .WithDescription("Returns every album with its song count, review count and average rating across all reviews of its songs.")
            .Produces<List<AlbumDto>>();

        group.MapGet("/{albumId:int}", GetById)
            .WithName("GetAlbum")
            .WithSummary("Get one album")
            .Produces<AlbumDto>()
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

    private static async Task<IResult> GetAll(GlazeAudioDbContext db, CancellationToken ct) =>
        Results.Ok(await Project(db.Albums.OrderBy(a => a.Id)).ToListAsync(ct));

    private static async Task<IResult> GetById(int albumId, GlazeAudioDbContext db, CancellationToken ct)
    {
        var album = await Project(db.Albums.Where(a => a.Id == albumId)).FirstOrDefaultAsync(ct);
        return album is null ? ApiProblems.NotFound("Album", albumId) : Results.Ok(album);
    }

    private static async Task<IResult> Create(AlbumRequest request, GlazeAudioDbContext db, CancellationToken ct)
    {
        var album = new Album
        {
            Title = request.Title!.Trim(),
            Artist = request.Artist!.Trim(),
            ReleaseYear = request.ReleaseYear!.Value,
            Genre = request.Genre!.Trim(),
            CoverUrl = request.CoverUrl
        };
        db.Albums.Add(album);
        await db.SaveChangesAsync(ct);

        var dto = new AlbumDto(album.Id, album.Title, album.Artist, album.ReleaseYear, album.Genre, album.CoverUrl, 0, 0, null);
        return Results.CreatedAtRoute("GetAlbum", new { albumId = album.Id }, dto);
    }

    private static async Task<IResult> Update(int albumId, AlbumRequest request, GlazeAudioDbContext db, CancellationToken ct)
    {
        var album = await db.Albums.FindAsync([albumId], ct);
        if (album is null) return ApiProblems.NotFound("Album", albumId);

        album.Title = request.Title!.Trim();
        album.Artist = request.Artist!.Trim();
        album.ReleaseYear = request.ReleaseYear!.Value;
        album.Genre = request.Genre!.Trim();
        album.CoverUrl = request.CoverUrl;
        await db.SaveChangesAsync(ct);

        return Results.Ok(await Project(db.Albums.Where(a => a.Id == albumId)).FirstAsync(ct));
    }

    private static async Task<IResult> Delete(int albumId, GlazeAudioDbContext db, CancellationToken ct)
    {
        var deleted = await db.Albums.Where(a => a.Id == albumId).ExecuteDeleteAsync(ct);
        return deleted == 0 ? ApiProblems.NotFound("Album", albumId) : Results.NoContent();
    }
}
