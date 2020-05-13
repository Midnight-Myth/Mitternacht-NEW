using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class VoiceStats_Guild : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VoiceChannelStats_UserId",
                table: "VoiceChannelStats");

            migrationBuilder.AddColumn<ulong>(
                name: "GuildId",
                table: "VoiceChannelStats",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChannelStats_UserId_GuildId",
                table: "VoiceChannelStats",
                columns: new[] { "UserId", "GuildId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VoiceChannelStats_UserId_GuildId",
                table: "VoiceChannelStats");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "VoiceChannelStats");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChannelStats_UserId",
                table: "VoiceChannelStats",
                column: "UserId",
                unique: true);
        }
    }
}
