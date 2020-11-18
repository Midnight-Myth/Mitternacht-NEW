using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class RenamePermissionv2ToPermission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissionv2_GuildConfigs_GuildConfigId",
                table: "Permissionv2");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permissionv2",
                table: "Permissionv2");

            migrationBuilder.RenameTable(
                name: "Permissionv2",
                newName: "Permission");

            migrationBuilder.RenameIndex(
                name: "IX_Permissionv2_GuildConfigId",
                table: "Permission",
                newName: "IX_Permission_GuildConfigId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Permission",
                table: "Permission",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Permission_GuildConfigs_GuildConfigId",
                table: "Permission",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permission_GuildConfigs_GuildConfigId",
                table: "Permission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permission",
                table: "Permission");

            migrationBuilder.RenameTable(
                name: "Permission",
                newName: "Permissionv2");

            migrationBuilder.RenameIndex(
                name: "IX_Permission_GuildConfigId",
                table: "Permissionv2",
                newName: "IX_Permissionv2_GuildConfigId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Permissionv2",
                table: "Permissionv2",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissionv2_GuildConfigs_GuildConfigId",
                table: "Permissionv2",
                column: "GuildConfigId",
                principalTable: "GuildConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
