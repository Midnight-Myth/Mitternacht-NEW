using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Mitternacht.Migrations
{
    public partial class usernamehistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LogUsernameHistory",
                table: "GuildConfigs",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LogUsernames",
                table: "BotConfig",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UsernameHistory",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(nullable: true),
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    DateReplaced = table.Column<DateTime>(nullable: true),
                    DateSet = table.Column<DateTime>(nullable: false),
                    Discriminator = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    UserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsernameHistory", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsernameHistory");

            migrationBuilder.DropColumn(
                name: "LogUsernameHistory",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "LogUsernames",
                table: "BotConfig");
        }
    }
}
