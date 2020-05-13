using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Mitternacht.Migrations
{
    public partial class BirthdayGC : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BirthdayMessage",
                table: "GuildConfigs",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "BirthdayMessageChannelId",
                table: "GuildConfigs",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BirthdaysEnabled",
                table: "GuildConfigs",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthdayMessage",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "BirthdayMessageChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "BirthdaysEnabled",
                table: "GuildConfigs");
        }
    }
}
