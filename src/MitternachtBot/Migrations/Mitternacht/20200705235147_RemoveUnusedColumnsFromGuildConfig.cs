using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class RemoveUnusedColumnsFromGuildConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CleverbotEnabled",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultMusicVolume",
                table: "GuildConfigs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CleverbotEnabled",
                table: "GuildConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<float>(
                name: "DefaultMusicVolume",
                table: "GuildConfigs",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
