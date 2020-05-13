using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Mitternacht.Migrations
{
    public partial class birthdatemodel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BirthDates",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    Day = table.Column<int>(nullable: false),
                    Month = table.Column<int>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    Year = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BirthDates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BirthDates_UserId",
                table: "BirthDates",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BirthDates");
        }
    }
}
