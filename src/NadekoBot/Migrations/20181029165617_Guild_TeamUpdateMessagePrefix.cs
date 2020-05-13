using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class Guild_TeamUpdateMessagePrefix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "TeamUpdateChannelId",
                table: "GuildConfigs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamUpdateMessagePrefix",
                table: "GuildConfigs",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamUpdateChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "TeamUpdateMessagePrefix",
                table: "GuildConfigs");
        }
    }
}
