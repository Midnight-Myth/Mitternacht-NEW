using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class levelupdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LevelModel_UserId",
                table: "LevelModel");

            migrationBuilder.AddColumn<ulong>(
                name: "GuildId",
                table: "LevelModel",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<int>(
                name: "MessageXpCharCountMax",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MessageXpCharCountMin",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "MessageXpTimeDifference",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TurnToXpMultiplier",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_LevelModel_GuildId_UserId",
                table: "LevelModel",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LevelModel_GuildId_UserId",
                table: "LevelModel");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "LevelModel");

            migrationBuilder.DropColumn(
                name: "MessageXpCharCountMax",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "MessageXpCharCountMin",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "MessageXpTimeDifference",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "TurnToXpMultiplier",
                table: "GuildConfigs");

            migrationBuilder.CreateIndex(
                name: "IX_LevelModel_UserId",
                table: "LevelModel",
                column: "UserId",
                unique: true);
        }
    }
}
