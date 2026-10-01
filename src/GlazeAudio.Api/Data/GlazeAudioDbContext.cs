using GlazeAudio.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlazeAudio.Api.Data;

public class GlazeAudioDbContext(DbContextOptions<GlazeAudioDbContext> options) : DbContext(options)
{
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Album>(album =>
        {
            album.Property(a => a.Title).HasMaxLength(200);
            album.Property(a => a.Artist).HasMaxLength(200);
            album.Property(a => a.Genre).HasMaxLength(100);
            album.Property(a => a.CoverUrl).HasMaxLength(500);

            album.HasMany(a => a.Songs)
                 .WithOne(s => s.Album)
                 .HasForeignKey(s => s.AlbumId)
                 .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Song>(song =>
        {
            song.Property(s => s.Title).HasMaxLength(200);

            song.HasMany(s => s.Reviews)
                .WithOne(r => r.Song)
                .HasForeignKey(r => r.SongId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Review>(review =>
        {
            review.Property(r => r.Comment).HasMaxLength(2000);
            review.Ignore(r => r.OverallRating);

            // A user can review a given song only once (they edit their review instead).
            review.HasIndex(r => new { r.UserId, r.SongId }).IsUnique();
        });

        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Username).HasMaxLength(50);
            user.Property(u => u.Email).HasMaxLength(200);
            user.Property(u => u.PasswordHash).HasMaxLength(500);
            user.Property(u => u.Role).HasMaxLength(20);
            user.Property(u => u.Bio).HasMaxLength(500);

            user.HasIndex(u => u.Username).IsUnique();
            user.HasIndex(u => u.Email).IsUnique();

            // Deleting an account removes the reviews it wrote.
            user.HasMany(u => u.Reviews)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
