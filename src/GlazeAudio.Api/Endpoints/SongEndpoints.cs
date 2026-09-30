using GlazeAudio.Api.Contracts;
using GlazeAudio.Api.Data;
using GlazeAudio.Api.Infrastructure;
using GlazeAudio.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlazeAudio.Api.Endpoints;

public static class SongEndpoints
{
    public static RouteGroupBuilder MapSongEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/albums/{albumId:int}/songs").WithTags("Songs");

        group.MapGet("/", GetAll)
            .WithName("GetSongs")
            .WithSummary("List songs of an album")
            .WithDescription("Returns the album's songs ordered by track number. 404 if the album does not exist.")
            .Produces<List<SongDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{songId:int}", GetById)
            .WithName("GetSong")
            .WithSummary("Get one song")
            .WithDescription("404 if the album does not exist or the song does not belong to it.")
            .Produces<SongDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateSong")
            .WithSummary("Add a song to an album")
            .WithValidation<SongRequest>()
            .Produces<SongDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{songId:int}", Update)
            .WithName("UpdateSong")
            .WithSummary("Update a song")
            .WithValidation<SongRequest>()
            .Produces<SongDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{songId:int}", Delete)
            .WithName("DeleteSong")
            .WithSummary("Delete a song")
            .WithDescription("Deletes the song and all of its reviews. Returns 204 with no body.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static IQueryable<SongDto> Project(IQueryable<Song> songs) =>
        songs.Select(s => new SongDto(
            s.Id, s.AlbumId, s.Title, s.TrackNumber, s.DurationSeconds,
            s.Reviews.Count,
            s.Reviews.Average(r => (double?)(r.LyricsRating + r.MelodyRating + r.MoodRating + r.ExpressivenessRating) / 4.0)));

    private static async Task<IResult> GetAll(int albumId, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (!await db.Albums.AnyAsync(a => a.Id == albumId, ct))
            return ApiProblems.NotFound("Album", albumId);

        var songs = await Project(db.Songs.Where(s => s.AlbumId == albumId).OrderBy(s => s.TrackNumber).ThenBy(s => s.Id))
            .ToListAsync(ct);
        return Results.Ok(songs);
    }

    private static async Task<IResult> GetById(int albumId, int songId, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (!await db.Albums.AnyAsync(a => a.Id == albumId, ct))
            return ApiProblems.NotFound("Album", albumId);

        var song = await Project(db.Songs.Where(s => s.AlbumId == albumId && s.Id == songId)).FirstOrDefaultAsync(ct);
        return song is null ? ApiProblems.NotFound("Song", songId) : Results.Ok(song);
    }

    private static async Task<IResult> Create(int albumId, SongRequest request, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (!await db.Albums.AnyAsync(a => a.Id == albumId, ct))
            return ApiProblems.NotFound("Album", albumId);

        var song = new Song
        {
            AlbumId = albumId,
            Title = request.Title!.Trim(),
            TrackNumber = request.TrackNumber!.Value,
            DurationSeconds = request.DurationSeconds!.Value
        };
        db.Songs.Add(song);
        await db.SaveChangesAsync(ct);

        var dto = new SongDto(song.Id, albumId, song.Title, song.TrackNumber, song.DurationSeconds, 0, null);
        return Results.CreatedAtRoute("GetSong", new { albumId, songId = song.Id }, dto);
    }

    private static async Task<IResult> Update(int albumId, int songId, SongRequest request, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (!await db.Albums.AnyAsync(a => a.Id == albumId, ct))
            return ApiProblems.NotFound("Album", albumId);

        var song = await db.Songs.FirstOrDefaultAsync(s => s.AlbumId == albumId && s.Id == songId, ct);
        if (song is null) return ApiProblems.NotFound("Song", songId);

        song.Title = request.Title!.Trim();
        song.TrackNumber = request.TrackNumber!.Value;
        song.DurationSeconds = request.DurationSeconds!.Value;
        await db.SaveChangesAsync(ct);

        return Results.Ok(await Project(db.Songs.Where(s => s.Id == songId)).FirstAsync(ct));
    }

    private static async Task<IResult> Delete(int albumId, int songId, GlazeAudioDbContext db, CancellationToken ct)
    {
        if (!await db.Albums.AnyAsync(a => a.Id == albumId, ct))
            return ApiProblems.NotFound("Album", albumId);

        var deleted = await db.Songs.Where(s => s.AlbumId == albumId && s.Id == songId).ExecuteDeleteAsync(ct);
        return deleted == 0 ? ApiProblems.NotFound("Song", songId) : Results.NoContent();
    }
}
