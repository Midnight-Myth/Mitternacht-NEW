using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class RemoveMusicModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaylistSong");

            migrationBuilder.DropTable(
                name: "MusicPlaylists");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicPlaylists",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Author = table.Column<string>(nullable: true),
                    AuthorId = table.Column<ulong>(nullable: false),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPlaylists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistSong",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    MusicPlaylistId = table.Column<int>(nullable: true),
                    Provider = table.Column<string>(nullable: true),
                    ProviderType = table.Column<int>(nullable: false),
                    Query = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    Uri = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistSong", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistSong_MusicPlaylists_MusicPlaylistId",
                        column: x => x.MusicPlaylistId,
                        principalTable: "MusicPlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistSong_MusicPlaylistId",
                table: "PlaylistSong",
                column: "MusicPlaylistId");
        }
    }
}
