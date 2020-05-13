using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Mitternacht.Migrations
{
    public partial class birthdayevents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "BirthdayRoleId",
                table: "GuildConfigs",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTimeBirthdaysChecked",
                table: "BotConfig",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthdayRoleId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "LastTimeBirthdaysChecked",
                table: "BotConfig");
        }
    }
}
