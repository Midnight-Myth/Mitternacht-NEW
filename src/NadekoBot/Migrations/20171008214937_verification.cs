using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class verification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "VerifiedRoleId",
                table: "GuildConfigs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifyString",
                table: "GuildConfigs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VerificatedUsers",
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
                    table.PrimaryKey("PK_VerificatedUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificatedUsers_GuildId_UserId",
                table: "VerificatedUsers",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificatedUsers");

            migrationBuilder.DropColumn(
                name: "VerifiedRoleId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "VerifyString",
                table: "GuildConfigs");
        }
    }
}
