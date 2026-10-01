using GlazeAudio.Api.Contracts;
using GlazeAudio.Api.Data;
using GlazeAudio.Api.Infrastructure;
using GlazeAudio.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GlazeAudio.Api.Endpoints;

/// <summary>
/// User accounts. Authentication (login, JWT) is not added yet, so these endpoints are open for now;
/// later, listing/deleting users will be admin-only and editing will be limited to your own account.
/// </summary>
public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/", GetAll)
            .WithName("GetUsers")
            .WithSummary("List users (paged, filterable)")
            .WithDescription("Filter with search (username or email) and role (User / Admin).")
            .Produces<PagedResult<UserDto>>()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{userId:int}", GetById)
            .WithName("GetUser")
            .WithSummary("Get one user")
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{userId:int}/reviews", GetReviews)
            .WithName("GetUserReviews")
            .WithSummary("List a user's reviews (paged)")
            .WithDescription(
                "Every review the user has written, newest first, with the song and album each one belongs to. " +
                "Filter with minRating.")
            .Produces<PagedResult<UserReviewDto>>()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateUser")
            .WithSummary("Register a user")
            .WithDescription(
                "Creates an account with the User role. The password is stored as a salted hash and never returned. " +
                "409 if the username or email is already taken.")
            .WithValidation<CreateUserRequest>()
            .Produces<UserDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{userId:int}", Update)
            .WithName("UpdateUser")
            .WithSummary("Update a user")
            .WithDescription("Replaces username, email, bio and role. 409 if the new username or email is already taken.")
            .WithValidation<UpdateUserRequest>()
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{userId:int}", Delete)
            .WithName("DeleteUser")
            .WithSummary("Delete a user")
            .WithDescription("Deletes the account together with all reviews it wrote. Returns 204 with no body.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static IQueryable<UserDto> Project(IQueryable<User> users) =>
        users.Select(u => new UserDto(u.Id, u.Username, u.Email, u.Role, u.Bio, u.CreatedAt, u.Reviews.Count));

    private static UserDto WithLinks(UserDto user, HttpRequest request) =>
        user with { Links = ApiLinks.User(request, user.Id) };

    /// <summary>Returns a 409 result if the username or email belongs to another account.</summary>
    private static async Task<IResult?> CheckUnique(GlazeAudioDbContext db, string username, string email, int? exceptUserId, CancellationToken ct)
    {
        var lowerName = username.ToLower();
        var lowerEmail = email.ToLower();

        if (await db.Users.AnyAsync(u => u.Id != exceptUserId && u.Username.ToLower() == lowerName, ct))
            return ApiProblems.Conflict("Username already taken.", $"The username '{username}' is already in use.");
        if (await db.Users.AnyAsync(u => u.Id != exceptUserId && u.Email.ToLower() == lowerEmail, ct))
            return ApiProblems.Conflict("Email already registered.", $"The email '{email}' is already in use.");
        return null;
    }

    private static async Task<IResult> GetAll([AsParameters] UserQuery query, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        if (Paging.Validate(query.Page, query.PageSize, out var page, out var pageSize) is { } invalid)
            return invalid;

        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            users = users.Where(u => u.Username.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = query.Role.Trim().ToLower();
            users = users.Where(u => u.Role.ToLower() == role);
        }

        var total = await users.CountAsync(ct);
        var items = await Project(users.OrderBy(u => u.Id).Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);

        return Results.Ok(Paging.Create(request, items.Select(u => WithLinks(u, request)).ToList(), page, pageSize, total));
    }

    private static async Task<IResult> GetById(int userId, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        var user = await Project(db.Users.Where(u => u.Id == userId)).FirstOrDefaultAsync(ct);
        return user is null ? ApiProblems.NotFound("User", userId) : Results.Ok(WithLinks(user, request));
    }

    private static async Task<IResult> GetReviews(int userId, [AsParameters] UserReviewQuery query, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        if (Paging.Validate(query.Page, query.PageSize, out var page, out var pageSize) is { } invalid)
            return invalid;
        if (query.MinRating is < 0 or > 5)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["minRating"] = ["minRating must be between 0 and 5."] },
                statusCode: StatusCodes.Status400BadRequest, title: "Invalid query parameters.");
        }
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
            return ApiProblems.NotFound("User", userId);

        var reviews = db.Reviews.AsNoTracking().Where(r => r.UserId == userId);
        if (query.MinRating is { } minRating)
        {
            var minSum = minRating * 4;
            reviews = reviews.Where(r => r.LyricsRating + r.MelodyRating + r.MoodRating + r.ExpressivenessRating >= minSum);
        }

        var total = await reviews.CountAsync(ct);
        var rows = await reviews
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new
            {
                r.Id, r.SongId, r.UserId, SongTitle = r.Song.Title,
                r.Song.AlbumId, AlbumTitle = r.Song.Album.Title, AlbumArtist = r.Song.Album.Artist,
                RatingSum = r.LyricsRating + r.MelodyRating + r.MoodRating + r.ExpressivenessRating,
                r.Comment, r.CreatedAt, r.UpdatedAt
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new UserReviewDto(
                r.Id, r.AlbumId, r.AlbumTitle, r.AlbumArtist, r.SongId, r.SongTitle,
                r.RatingSum / 4.0, r.Comment, r.CreatedAt, r.UpdatedAt)
            {
                Links = ApiLinks.Review(request, r.AlbumId, r.SongId, r.Id, r.UserId)
            })
            .ToList();

        return Results.Ok(Paging.Create(request, items, page, pageSize, total));
    }

    private static async Task<IResult> Create(CreateUserRequest body, GlazeAudioDbContext db, IPasswordHasher<User> hasher, HttpRequest request, CancellationToken ct)
    {
        var username = body.Username!.Trim();
        var email = body.Email!.Trim();
        if (await CheckUnique(db, username, email, null, ct) is { } conflict) return conflict;

        var user = new User
        {
            Username = username,
            Email = email,
            Bio = string.IsNullOrWhiteSpace(body.Bio) ? null : body.Bio.Trim(),
            Role = UserRoles.User
        };
        user.PasswordHash = hasher.HashPassword(user, body.Password!);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var dto = new UserDto(user.Id, user.Username, user.Email, user.Role, user.Bio, user.CreatedAt, 0);
        return Results.CreatedAtRoute("GetUser", new { userId = user.Id }, WithLinks(dto, request));
    }

    private static async Task<IResult> Update(int userId, UpdateUserRequest body, GlazeAudioDbContext db, HttpRequest request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return ApiProblems.NotFound("User", userId);

        var username = body.Username!.Trim();
        var email = body.Email!.Trim();
        if (await CheckUnique(db, username, email, userId, ct) is { } conflict) return conflict;

        user.Username = username;
        user.Email = email;
        user.Bio = string.IsNullOrWhiteSpace(body.Bio) ? null : body.Bio.Trim();
        user.Role = body.Role!;
        await db.SaveChangesAsync(ct);

        var dto = await Project(db.Users.Where(u => u.Id == userId)).FirstAsync(ct);
        return Results.Ok(WithLinks(dto, request));
    }

    private static async Task<IResult> Delete(int userId, GlazeAudioDbContext db, CancellationToken ct)
    {
        var deleted = await db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(ct);
        return deleted == 0 ? ApiProblems.NotFound("User", userId) : Results.NoContent();
    }
}
