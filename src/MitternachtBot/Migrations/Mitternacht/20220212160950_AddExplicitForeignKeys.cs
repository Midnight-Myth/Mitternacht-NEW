using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class AddExplicitForeignKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuildConfigs_LogSetting_LogSettingId",
                table: "GuildConfigs");

            migrationBuilder.AlterColumn<int>(
                name: "LogSettingId",
                table: "GuildConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GuildConfigs_LogSetting_LogSettingId",
                table: "GuildConfigs",
                column: "LogSettingId",
                principalTable: "LogSetting",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuildConfigs_LogSetting_LogSettingId",
                table: "GuildConfigs");

            migrationBuilder.AlterColumn<int>(
                name: "LogSettingId",
                table: "GuildConfigs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_GuildConfigs_LogSetting_LogSettingId",
                table: "GuildConfigs",
                column: "LogSettingId",
                principalTable: "LogSetting",
                principalColumn: "Id");
        }
    }
}
