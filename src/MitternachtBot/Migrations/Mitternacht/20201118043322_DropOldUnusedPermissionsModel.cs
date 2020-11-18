using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class DropOldUnusedPermissionsModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuildConfigs_Permission_RootPermissionId",
                table: "GuildConfigs");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropIndex(
                name: "IX_GuildConfigs_RootPermissionId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "RootPermissionId",
                table: "GuildConfigs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RootPermissionId",
                table: "GuildConfigs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NextId = table.Column<int>(type: "integer", nullable: true),
                    PrimaryTarget = table.Column<int>(type: "integer", nullable: false),
                    PrimaryTargetId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    SecondaryTarget = table.Column<int>(type: "integer", nullable: false),
                    SecondaryTargetName = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permission_Permission_NextId",
                        column: x => x.NextId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_RootPermissionId",
                table: "GuildConfigs",
                column: "RootPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_NextId",
                table: "Permission",
                column: "NextId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GuildConfigs_Permission_RootPermissionId",
                table: "GuildConfigs",
                column: "RootPermissionId",
                principalTable: "Permission",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
