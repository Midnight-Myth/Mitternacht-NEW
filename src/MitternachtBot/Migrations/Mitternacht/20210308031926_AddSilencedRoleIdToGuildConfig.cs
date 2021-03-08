using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class AddSilencedRoleIdToGuildConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SilencedRoleId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SilencedRoleId",
                table: "GuildConfigs");
        }
    }
}
