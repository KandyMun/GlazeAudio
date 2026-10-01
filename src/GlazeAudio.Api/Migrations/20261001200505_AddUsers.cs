using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlazeAudio.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUsers : Migration
    {
        /// <summary>
        /// Adds user accounts and links every review to its author.
        /// Existing data is kept: an account is created for each distinct AuthorName already in Reviews
        /// (with an empty password – the app's seeding gives the demo accounts their password), every review
        /// is linked to that account, and only then is the old AuthorName column removed.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Users table
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            // 2. Reviews.UserId, nullable until it has been filled in
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Reviews",
                type: "int",
                nullable: true);

            // 3. One account per distinct existing author name (+ a fallback for blank names)
            migrationBuilder.Sql(@"
INSERT INTO [Users] ([Username], [Email], [PasswordHash], [Role], [Bio], [CreatedAt])
SELECT a.[Name], a.[Name] + N'@glazeaudio.test', N'', N'User', NULL, SYSUTCDATETIME()
FROM (SELECT DISTINCT LEFT(LTRIM(RTRIM([AuthorName])), 50) AS [Name] FROM [Reviews]) AS a
WHERE a.[Name] <> N'';");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [Reviews] WHERE LTRIM(RTRIM([AuthorName])) = N'')
   AND NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = N'unknown_author')
    INSERT INTO [Users] ([Username], [Email], [PasswordHash], [Role], [Bio], [CreatedAt])
    VALUES (N'unknown_author', N'unknown_author@glazeaudio.test', N'', N'User', NULL, SYSUTCDATETIME());");

            // 4. Link every review to its author's account
            migrationBuilder.Sql(@"
UPDATE r SET r.[UserId] = u.[Id]
FROM [Reviews] AS r
INNER JOIN [Users] AS u ON u.[Username] = LEFT(LTRIM(RTRIM(r.[AuthorName])), 50);");

            migrationBuilder.Sql(@"
UPDATE [Reviews]
SET [UserId] = (SELECT [Id] FROM [Users] WHERE [Username] = N'unknown_author')
WHERE [UserId] IS NULL;");

            // 5. One review per user per song: if an author reviewed the same song twice, keep the newest
            migrationBuilder.Sql(@"
WITH ranked AS (
    SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [UserId], [SongId] ORDER BY [CreatedAt] DESC, [Id] DESC) AS rn
    FROM [Reviews]
)
DELETE FROM ranked WHERE rn > 1;");

            // 6. Now UserId can be required, and the old text column can go
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Reviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "Reviews");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId_SongId",
                table: "Reviews",
                columns: new[] { "UserId", "SongId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_UserId",
                table: "Reviews",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <summary>Puts the author's username back into Reviews.AuthorName, then removes the Users table.</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "Reviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE r SET r.[AuthorName] = u.[Username]
FROM [Reviews] AS r
INNER JOIN [Users] AS u ON u.[Id] = r.[UserId];");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorName",
                table: "Reviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Users_UserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_UserId_SongId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Reviews");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
