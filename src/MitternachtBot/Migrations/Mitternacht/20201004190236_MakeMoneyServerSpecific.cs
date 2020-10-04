using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class MakeMoneyServerSpecific : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoleMoney_RoleId",
                table: "RoleMoney");

            migrationBuilder.DropIndex(
                name: "IX_RoleLevelBinding_RoleId",
                table: "RoleLevelBinding");

            migrationBuilder.DropIndex(
                name: "IX_DailyMoney_UserId",
                table: "DailyMoney");

            migrationBuilder.DropIndex(
                name: "IX_Currency_UserId",
                table: "Currency");

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "RoleMoney",
                nullable: false,
                defaultValue: 147439310096826368m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "RoleLevelBinding",
                nullable: false,
                defaultValue: 147439310096826368m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "DailyMoneyStats",
                nullable: false,
                defaultValue: 147439310096826368m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "DailyMoney",
                nullable: false,
                defaultValue: 147439310096826368m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "CurrencyTransactions",
                nullable: false,
                defaultValue: 147439310096826368m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "Currency",
                nullable: false,
                defaultValue: 147439310096826368m);

            migrationBuilder.CreateIndex(
                name: "IX_RoleMoney_GuildId_RoleId",
                table: "RoleMoney",
                columns: new[] { "GuildId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleLevelBinding_GuildId_RoleId",
                table: "RoleLevelBinding",
                columns: new[] { "GuildId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyMoney_GuildId_UserId",
                table: "DailyMoney",
                columns: new[] { "GuildId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currency_GuildId_UserId",
                table: "Currency",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoleMoney_GuildId_RoleId",
                table: "RoleMoney");

            migrationBuilder.DropIndex(
                name: "IX_RoleLevelBinding_GuildId_RoleId",
                table: "RoleLevelBinding");

            migrationBuilder.DropIndex(
                name: "IX_DailyMoney_GuildId_UserId",
                table: "DailyMoney");

            migrationBuilder.DropIndex(
                name: "IX_Currency_GuildId_UserId",
                table: "Currency");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "RoleMoney");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "RoleLevelBinding");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "DailyMoneyStats");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "DailyMoney");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "CurrencyTransactions");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "Currency");

            migrationBuilder.CreateIndex(
                name: "IX_RoleMoney_RoleId",
                table: "RoleMoney",
                column: "RoleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleLevelBinding_RoleId",
                table: "RoleLevelBinding",
                column: "RoleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyMoney_UserId",
                table: "DailyMoney",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currency_UserId",
                table: "Currency",
                column: "UserId",
                unique: true);
        }
    }
}
