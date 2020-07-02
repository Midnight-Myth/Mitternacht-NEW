using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations
{
    public partial class RemoveWaifuModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaifuItem");

            migrationBuilder.DropTable(
                name: "WaifuUpdates");

            migrationBuilder.DropTable(
                name: "WaifuInfo");

            migrationBuilder.DropTable(
                name: "DiscordUser");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordUser", x => x.Id);
                    table.UniqueConstraint("AK_DiscordUser_UserId", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "WaifuInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AffinityId = table.Column<int>(type: "INTEGER", nullable: true),
                    ClaimerId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Price = table.Column<int>(type: "INTEGER", nullable: false),
                    WaifuId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaifuInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaifuInfo_DiscordUser_AffinityId",
                        column: x => x.AffinityId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaifuInfo_DiscordUser_ClaimerId",
                        column: x => x.ClaimerId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaifuInfo_DiscordUser_WaifuId",
                        column: x => x.WaifuId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaifuUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NewId = table.Column<int>(type: "INTEGER", nullable: true),
                    OldId = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateType = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaifuUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaifuUpdates_DiscordUser_NewId",
                        column: x => x.NewId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaifuUpdates_DiscordUser_OldId",
                        column: x => x.OldId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaifuUpdates_DiscordUser_UserId",
                        column: x => x.UserId,
                        principalTable: "DiscordUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaifuItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Item = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemEmoji = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<int>(type: "INTEGER", nullable: false),
                    WaifuInfoId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaifuItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaifuItem_WaifuInfo_WaifuInfoId",
                        column: x => x.WaifuInfoId,
                        principalTable: "WaifuInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaifuInfo_AffinityId",
                table: "WaifuInfo",
                column: "AffinityId");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuInfo_ClaimerId",
                table: "WaifuInfo",
                column: "ClaimerId");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuInfo_WaifuId",
                table: "WaifuInfo",
                column: "WaifuId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaifuItem_WaifuInfoId",
                table: "WaifuItem",
                column: "WaifuInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuUpdates_NewId",
                table: "WaifuUpdates",
                column: "NewId");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuUpdates_OldId",
                table: "WaifuUpdates",
                column: "OldId");

            migrationBuilder.CreateIndex(
                name: "IX_WaifuUpdates_UserId",
                table: "WaifuUpdates",
                column: "UserId");
        }
    }
}
