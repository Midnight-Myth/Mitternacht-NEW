using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class counttonumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "CountToNumberChannelId",
                table: "GuildConfigs",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CountToNumberMessageChance",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountToNumberChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "CountToNumberMessageChance",
                table: "GuildConfigs");
        }
    }
}
