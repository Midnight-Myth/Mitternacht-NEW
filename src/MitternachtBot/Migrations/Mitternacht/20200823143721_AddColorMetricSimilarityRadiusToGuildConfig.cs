using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class AddColorMetricSimilarityRadiusToGuildConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ColorMetricSimilarityRadius",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: 5.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorMetricSimilarityRadius",
                table: "GuildConfigs");
        }
    }
}
