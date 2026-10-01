namespace GlazeAudio.Api.Models;

/// <summary>A platform account. Not a domain object – it owns reviews and decides what a person may do.</summary>
public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }

    /// <summary>ASP.NET Core Identity password hash (PBKDF2). Never returned by the API.</summary>
    public string PasswordHash { get; set; } = "";

    /// <summary>"User" or "Admin" – see <see cref="UserRoles"/>. Will be put into the JWT as a role claim.</summary>
    public string Role { get; set; } = UserRoles.User;

    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Review> Reviews { get; set; } = [];
}

public static class UserRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
}
