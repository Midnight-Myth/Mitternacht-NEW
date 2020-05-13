using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class VerificationLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "VerificationMessages",
                table: "LogSettings",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "VerificationSteps",
                table: "LogSettings",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationMessages",
                table: "LogSettings");

            migrationBuilder.DropColumn(
                name: "VerificationSteps",
                table: "LogSettings");
        }
    }
}
