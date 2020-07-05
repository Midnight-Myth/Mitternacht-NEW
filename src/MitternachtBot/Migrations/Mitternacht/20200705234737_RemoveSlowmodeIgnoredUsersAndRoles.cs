using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class RemoveSlowmodeIgnoredUsersAndRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlowmodeIgnoredRole");

            migrationBuilder.DropTable(
                name: "SlowmodeIgnoredUser");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlowmodeIgnoredRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuildConfigId = table.Column<int>(type: "integer", nullable: true),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlowmodeIgnoredRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlowmodeIgnoredRole_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlowmodeIgnoredUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuildConfigId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlowmodeIgnoredUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlowmodeIgnoredUser_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlowmodeIgnoredRole_GuildConfigId",
                table: "SlowmodeIgnoredRole",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_SlowmodeIgnoredUser_GuildConfigId",
                table: "SlowmodeIgnoredUser",
                column: "GuildConfigId");
        }
    }
}
