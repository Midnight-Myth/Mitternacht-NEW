using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class AddColumnHiddenToWarnings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Hidden",
                table: "Warnings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hidden",
                table: "Warnings");
        }
    }
}
