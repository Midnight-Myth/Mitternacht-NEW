using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class AddModerationPointColumnsToWarning : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Points",
                table: "Warnings",
                newName: "PointsMedium");

            migrationBuilder.AddColumn<long>(
                name: "PointsHard",
                table: "Warnings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PointsLight",
                table: "Warnings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PointsHard",
                table: "Warnings");

            migrationBuilder.DropColumn(
                name: "PointsLight",
                table: "Warnings");

            migrationBuilder.RenameColumn(
                name: "PointsMedium",
                table: "Warnings",
                newName: "Points");
        }
    }
}
