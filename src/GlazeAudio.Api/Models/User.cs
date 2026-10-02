namespace GlazeAudio.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }

    /// <summary>ASP.NET Core Identity password hash (PBKDF2). Never returned by the API.</summary>
    public string PasswordHash { get; set; } = "";

    /// <summary>"User" or "Admin".</summary>
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
