using Microsoft.EntityFrameworkCore.Migrations;

namespace Mitternacht.Migrations.Mitternacht
{
    public partial class AddUniqueIndexToWatchedForumAccounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WatchedForumAccounts_GuildId_ForumUserId",
                table: "WatchedForumAccounts",
                columns: new[] { "GuildId", "ForumUserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WatchedForumAccounts_GuildId_ForumUserId",
                table: "WatchedForumAccounts");
        }
    }
}
