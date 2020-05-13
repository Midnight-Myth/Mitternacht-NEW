using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class rolemoney : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyMoney",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    LastTimeGotten = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMoney", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LevelModel",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrentXP = table.Column<int>(nullable: false),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Level = table.Column<int>(nullable: false),
                    TotalXP = table.Column<int>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    timestamp = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelModel", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyMoney_UserId",
                table: "DailyMoney",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LevelModel_UserId",
                table: "LevelModel",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyMoney");

            migrationBuilder.DropTable(
                name: "LevelModel");
        }
    }
}
