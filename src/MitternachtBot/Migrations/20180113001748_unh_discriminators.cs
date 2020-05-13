using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Mitternacht.Migrations
{
    public partial class unh_discriminators : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ushort>(
                name: "DiscordDiscriminator",
                table: "UsernameHistory",
                nullable: false,
                defaultValue: (ushort)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordDiscriminator",
                table: "UsernameHistory");
        }
    }
}
