using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class RemovePokemonModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PokeGame");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PokeGame",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokeGame", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PokeGame_UserId",
                table: "PokeGame",
                column: "UserId",
                unique: true);
        }
    }
}
