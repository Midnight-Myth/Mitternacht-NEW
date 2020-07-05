using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class RemoveUnusedColumnsFromBotConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BufferSize",
                table: "BotConfig");

            migrationBuilder.DropColumn(
                name: "TriviaCurrencyReward",
                table: "BotConfig");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BufferSize",
                table: "BotConfig",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TriviaCurrencyReward",
                table: "BotConfig",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
