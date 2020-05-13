using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class GuildConfig_ZalgoFilter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FilterZalgo",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ZalgoFilterChannel",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(nullable: false),
                    DateAdded = table.Column<DateTime>(nullable: true),
                    GuildConfigId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZalgoFilterChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZalgoFilterChannel_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZalgoFilterChannel_GuildConfigId",
                table: "ZalgoFilterChannel",
                column: "GuildConfigId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZalgoFilterChannel");

            migrationBuilder.DropColumn(
                name: "FilterZalgo",
                table: "GuildConfigs");
        }
    }
}
