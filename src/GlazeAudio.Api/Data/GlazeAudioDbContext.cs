using GlazeAudio.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlazeAudio.Api.Data;

public class GlazeAudioDbContext(DbContextOptions<GlazeAudioDbContext> options) : DbContext(options)
{
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<Review> Reviews => Set<Review>();

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
            review.Property(r => r.AuthorName).HasMaxLength(100);
            review.Property(r => r.Comment).HasMaxLength(2000);
            review.Ignore(r => r.OverallRating);
        });
    }
}
