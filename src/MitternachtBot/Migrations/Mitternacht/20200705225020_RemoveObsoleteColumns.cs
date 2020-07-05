using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class RemoveObsoleteColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandPrice");

            migrationBuilder.DropTable(
                name: "ModulePrefix");

            migrationBuilder.DropColumn(
                name: "ChannelCreated",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "ChannelDestroyed",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "ChannelUpdated",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "IsLogging",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "LogUserPresence",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "LogVoicePresence",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "MessageDeleted",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "MessageUpdated",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "UserBanned",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "UserJoined",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "UserLeft",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "UserPresenceChannelId",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "UserUnbanned",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "UserUpdated",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "VoicePresenceChannelId",
                table: "LogSetting");

            migrationBuilder.DropColumn(
                name: "CurrentXP",
                table: "LevelModel");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "LevelModel");

            migrationBuilder.DropColumn(
                name: "AutoDeleteByeMessages",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "AutoDeleteGreetMessages",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "SupportChannelId",
                table: "GuildConfigs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChannelCreated",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ChannelDestroyed",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ChannelId",
                table: "LogSetting",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ChannelUpdated",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLogging",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LogUserPresence",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LogVoicePresence",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MessageDeleted",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MessageUpdated",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UserBanned",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UserJoined",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UserLeft",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "UserPresenceChannelId",
                table: "LogSetting",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "UserUnbanned",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UserUpdated",
                table: "LogSetting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "VoicePresenceChannelId",
                table: "LogSetting",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CurrentXP",
                table: "LevelModel",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "LevelModel",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AutoDeleteByeMessages",
                table: "GuildConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoDeleteGreetMessages",
                table: "GuildConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SupportChannelId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommandPrice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BotConfigId = table.Column<int>(type: "integer", nullable: true),
                    CommandName = table.Column<string>(type: "text", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Price = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandPrice_BotConfig_BotConfigId",
                        column: x => x.BotConfigId,
                        principalTable: "BotConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModulePrefix",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BotConfigId = table.Column<int>(type: "integer", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ModuleName = table.Column<string>(type: "text", nullable: true),
                    Prefix = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModulePrefix", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModulePrefix_BotConfig_BotConfigId",
                        column: x => x.BotConfigId,
                        principalTable: "BotConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommandPrice_BotConfigId",
                table: "CommandPrice",
                column: "BotConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandPrice_Price",
                table: "CommandPrice",
                column: "Price",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModulePrefix_BotConfigId",
                table: "ModulePrefix",
                column: "BotConfigId");
        }
    }
}
