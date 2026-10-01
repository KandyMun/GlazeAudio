using GlazeAudio.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GlazeAudio.Api.Data;

/// <summary>
/// Fills the database with demo accounts, albums, songs and reviews so the API has meaningful data
/// right after the first start (or after `dotnet ef database update`).
/// Runs every time but only adds what is missing, so it is safe on an already seeded database.
/// </summary>
public static class SeedData
{
    /// <summary>Password of every seeded account (demo data only).</summary>
    public const string DemoPassword = "GlazeAudio123!";

    private static readonly (string Username, string Role, string Bio)[] SeedUsers =
    [
        ("admin", UserRoles.Admin, "Keeps the catalogue tidy and removes harmful reviews."),
        ("mantas", UserRoles.User, "Listens to everything from Radiohead to Kendrick."),
        ("vinyl_owl", UserRoles.User, "Collects records, mostly 70s and 90s."),
        ("bassline_ben", UserRoles.User, "Bass player. Judges every song by its low end."),
        ("quietstorm", UserRoles.User, "Prefers slow, moody albums."),
        ("dj_lina", UserRoles.User, "Club DJ who also likes a good ballad.")
    ];

    public static void Seed(DbContext context)
    {
        var db = (GlazeAudioDbContext)context;
        var users = EnsureUsers(db, db.Users.ToList());
        db.SaveChanges();

        if (db.Albums.Any()) return;
        db.Albums.AddRange(CreateAlbums(name => users[name]));
        db.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, CancellationToken ct)
    {
        var db = (GlazeAudioDbContext)context;
        var users = EnsureUsers(db, await db.Users.ToListAsync(ct));
        await db.SaveChangesAsync(ct);

        if (await db.Albums.AnyAsync(ct)) return;
        db.Albums.AddRange(CreateAlbums(name => users[name]));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Adds missing demo accounts and gives a password to accounts that have none
    /// (accounts created by the AddUsers migration from old review author names).
    /// </summary>
    private static Dictionary<string, User> EnsureUsers(GlazeAudioDbContext db, List<User> existing)
    {
        var hasher = new PasswordHasher<User>();
        var byName = existing.ToDictionary(u => u.Username, StringComparer.OrdinalIgnoreCase);

        foreach (var (username, role, bio) in SeedUsers)
        {
            if (!byName.TryGetValue(username, out var user))
            {
                user = new User { Username = username, Email = $"{username}@glazeaudio.test", Role = role, Bio = bio };
                db.Users.Add(user);
                byName[username] = user;
            }
            else if (string.IsNullOrEmpty(user.PasswordHash))
            {
                // Account created by the AddUsers migration from an old author name: finish setting it up.
                user.Role = role;
                user.Bio ??= bio;
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
                user.PasswordHash = hasher.HashPassword(user, DemoPassword);
        }
        return byName;
    }

    private static Review R(User author, int lyrics, int melody, int mood, int expressiveness, string comment) => new()
    {
        User = author,
        LyricsRating = lyrics,
        MelodyRating = melody,
        MoodRating = mood,
        ExpressivenessRating = expressiveness,
        Comment = comment
    };

    private static Song S(int track, string title, int seconds, params Review[] reviews) => new()
    {
        TrackNumber = track,
        Title = title,
        DurationSeconds = seconds,
        Reviews = [.. reviews]
    };

    private static List<Album> CreateAlbums(Func<string, User> user) =>
    [
        new Album
        {
            Title = "OK Computer", Artist = "Radiohead", ReleaseYear = 1997, Genre = "Alternative Rock",
            Songs =
            [
                S(1, "Airbag", 284,
                    R(user("mantas"), 4, 5, 4, 4, "Great opener. The drum loop and the bass dropping in and out keep it tense the whole time."),
                    R(user("vinyl_owl"), 3, 4, 4, 3, "Good energy, but the lyrics are too vague for me to connect with.")),
                S(2, "Paranoid Android", 383,
                    R(user("mantas"), 5, 5, 5, 5, "Three songs stitched into one and every part works. The guitar breakdown is still unreal."),
                    R(user("bassline_ben"), 4, 5, 4, 5, "Too long for casual listening, but you notice something new every time."),
                    R(user("quietstorm"), 4, 4, 3, 5, "Brilliant but exhausting. Not something I put on in the background.")),
                S(3, "Karma Police", 264,
                    R(user("vinyl_owl"), 4, 5, 4, 4, "The piano hook is simple and it sticks. The ending falls apart in the best way.")),
                S(4, "No Surprises", 229,
                    R(user("quietstorm"), 5, 5, 5, 4, "Sounds like a lullaby and reads like a cry for help. That contrast is the whole point."),
                    R(user("dj_lina"), 4, 5, 5, 3, "Very pretty, maybe too calm to hold my attention the whole way through."))
            ]
        },
        new Album
        {
            Title = "Random Access Memories", Artist = "Daft Punk", ReleaseYear = 2013, Genre = "Electronic / Disco",
            Songs =
            [
                S(1, "Give Life Back to Music", 274,
                    R(user("dj_lina"), 2, 5, 5, 4, "Almost no lyrics, all groove. The live instruments sound huge.")),
                S(2, "Instant Crush", 337,
                    R(user("mantas"), 4, 4, 4, 3, "Catchy, with a surprisingly sad undertone under the vocoder.")),
                S(3, "Get Lucky", 369,
                    R(user("bassline_ben"), 3, 5, 5, 4, "The guitar and bass lock together perfectly. Impossible not to move."),
                    R(user("vinyl_owl"), 2, 4, 5, 3, "Fun, but I got tired of it after the radio played it a thousand times.")),
                S(4, "Touch", 498,
                    R(user("quietstorm"), 4, 4, 4, 5, "The most ambitious song on the album. It goes through a lot of moods in eight minutes."))
            ]
        },
        new Album
        {
            Title = "To Pimp a Butterfly", Artist = "Kendrick Lamar", ReleaseYear = 2015, Genre = "Hip-Hop / Jazz Rap",
            Songs =
            [
                S(1, "Wesley's Theory", 287,
                    R(user("bassline_ben"), 4, 5, 4, 5, "The bass on this is ridiculous. A funky, confident way to start the album.")),
                S(2, "King Kunta", 234,
                    R(user("mantas"), 5, 4, 5, 5, "Confident and funky, with some of the sharpest lines on the record."),
                    R(user("dj_lina"), 4, 4, 5, 4, "The groove carries it even if you don't catch every reference.")),
                S(3, "Alright", 219,
                    R(user("quietstorm"), 5, 4, 5, 5, "Hopeful without ignoring how bad things are. The hook is chant-along material.")),
                S(4, "The Blacker the Berry", 328,
                    R(user("vinyl_owl"), 5, 3, 3, 5, "Angry and uncomfortable on purpose. The verses hit hard, the beat is intentionally rough."))
            ]
        },
        new Album
        {
            Title = "Rumours", Artist = "Fleetwood Mac", ReleaseYear = 1977, Genre = "Soft Rock",
            Songs =
            [
                S(1, "Go Your Own Way", 223,
                    R(user("vinyl_owl"), 4, 5, 4, 5, "A breakup song you can shout along to. The drums keep pushing it forward.")),
                S(2, "Dreams", 257,
                    R(user("quietstorm"), 4, 5, 5, 4, "Hypnotic and smooth. That bassline and the soft vocals make it timeless."),
                    R(user("mantas"), 4, 4, 5, 4, "Relaxing without being boring. I play it on long drives.")),
                S(3, "Songbird", 200),
                S(4, "The Chain", 270,
                    R(user("bassline_ben"), 3, 5, 4, 5, "Everyone waits for the bass solo before the ending, and it's worth it."))
            ]
        },
        new Album
        {
            Title = "Currents", Artist = "Tame Impala", ReleaseYear = 2015, Genre = "Psychedelic Pop",
            Songs =
            [
                S(1, "Let It Happen", 467,
                    R(user("dj_lina"), 4, 5, 5, 4, "The fake skipping record part in the middle got me the first time. Great build-up.")),
                S(2, "Eventually", 318),
                S(3, "The Less I Know the Better", 216,
                    R(user("mantas"), 3, 5, 4, 4, "That bassline carries everything. The story is silly, which fits the song."),
                    R(user("vinyl_owl"), 3, 4, 4, 3, "Catchy, but overplayed. Other tracks on the album deserve more attention."))
            ]
        }
    ];
}
