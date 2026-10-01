namespace GlazeAudio.Api.Models;

/// <summary>
/// A review of a single song. Each aspect is rated 0–5, and the comment explains the rating.
/// </summary>
public class Review
{
    public int Id { get; set; }
    public int SongId { get; set; }
    public Song Song { get; set; } = null!;

    /// <summary>The account that wrote the review. One review per user per song.</summary>
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int LyricsRating { get; set; }
    public int MelodyRating { get; set; }
    public int MoodRating { get; set; }
    public int ExpressivenessRating { get; set; }
    public required string Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public double OverallRating =>
        (LyricsRating + MelodyRating + MoodRating + ExpressivenessRating) / 4.0;
}
