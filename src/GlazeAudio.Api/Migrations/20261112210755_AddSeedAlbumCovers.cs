using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlazeAudio.Api.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Data-only migration: gives the five demo albums the cover images that the frontend ships
    /// in web/public/covers. Only albums that still have no cover are touched.
    /// </summary>
    public partial class AddSeedAlbumCovers : Migration
    {
        private static readonly (string Title, string Artist, string Cover)[] Covers =
        [
        ("OK Computer", "Radiohead", "/covers/cobalt-drip.jpg"),
        ("Random Access Memories", "Daft Punk", "/covers/ink-speckle.jpg"),
        ("To Pimp a Butterfly", "Kendrick Lamar", "/covers/ochre-crackle.jpg"),
        ("Rumours", "Fleetwood Mac", "/covers/celadon-pool.jpg"),
        ("Currents", "Tame Impala", "/covers/plum-flow.jpg"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (title, artist, cover) in Covers)
            {
                migrationBuilder.Sql(
                    $"UPDATE Albums SET CoverUrl = N'{cover}' WHERE Title = N'{title}' AND Artist = N'{artist}' AND CoverUrl IS NULL;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (title, artist, cover) in Covers)
            {
                migrationBuilder.Sql(
                    $"UPDATE Albums SET CoverUrl = NULL WHERE Title = N'{title}' AND Artist = N'{artist}' AND CoverUrl = N'{cover}';");
            }
        }
    }
}
