using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class RenameColumnLastTimeGottenToLastTimeReceived : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastTimeGotten",
                table: "DailyMoney",
                newName: "LastTimeReceived");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastTimeReceived",
                table: "DailyMoney",
                newName: "LastTimeGotten");
        }
    }
}
