using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class AddColumnPointsToWarnings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Points",
                table: "Warnings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "Warnings");
        }
    }
}
