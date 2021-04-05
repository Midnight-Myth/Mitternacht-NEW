using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class AddUniqueIndexOnVerifiedUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VerifiedUsers_GuildId_ForumUserId",
                table: "VerifiedUsers",
                columns: new[] { "GuildId", "ForumUserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerifiedUsers_GuildId_ForumUserId",
                table: "VerifiedUsers");
        }
    }
}
