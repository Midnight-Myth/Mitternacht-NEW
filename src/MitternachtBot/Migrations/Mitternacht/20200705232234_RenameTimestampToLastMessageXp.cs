using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class RenameTimestampToLastMessageXp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.RenameColumn(
				name: "timestamp",
				table: "LevelModel",
				newName: "LastMessageXp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.RenameColumn(
				name: "LastMessageXp",
				table: "LevelModel",
				newName: "timestamp");
		}
    }
}
