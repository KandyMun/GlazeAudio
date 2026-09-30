namespace GlazeAudio.Api.Models;

public class Album
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public int ReleaseYear { get; set; }
    public required string Genre { get; set; }
    public string? CoverUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Song> Songs { get; set; } = [];
}
