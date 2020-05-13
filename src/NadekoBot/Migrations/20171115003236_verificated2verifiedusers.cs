using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class verificated2verifiedusers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificatedUsers");

            migrationBuilder.CreateTable(
                name: "VerifiedUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ForumUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiedUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedUsers_GuildId_UserId",
                table: "VerifiedUsers",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerifiedUsers");

            migrationBuilder.CreateTable(
                name: "VerificatedUsers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    ForumUserId = table.Column<long>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificatedUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificatedUsers_GuildId_UserId",
                table: "VerificatedUsers",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }
    }
}
