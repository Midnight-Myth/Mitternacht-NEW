using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using GommeHDnetForumAPI.DataModels;
using Mitternacht.Modules.Forum.Services;
using Mitternacht.Services;

namespace Mitternacht.Modules.Verification.Services
{
    public class GommeTeamSyncService : INService
    {
        private readonly DbService _db;
        private readonly ForumService _fs;
        private readonly DiscordSocketClient _client;

        private Task _timerTask;
        private const int GommeTeamMemberCheckRepeatDelay = 60 * 1000;

        public GommeTeamSyncService(DbService db, ForumService fs, DiscordSocketClient client)
        {
            _db = db;
            _fs = fs;
            _client = client;

            _timerTask = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(GommeTeamMemberCheckRepeatDelay);
                    await CheckGommeTeamMembers().ConfigureAwait(false);
                }
            });
        }

        private async Task CheckGommeTeamMembers()
        {
            if (!_fs.LoggedIn) return;
            var staffIds = (await _fs.Forum.GetMembersList(MembersListType.Staff).ConfigureAwait(false)).Select(ui => ui.Id).ToList();
            using (var uow = _db.UnitOfWork)
            {
                var gcs = uow.GuildConfigs.GetAllGuildConfigs(_client.Guilds.Select(sg => sg.Id).ToList());
                foreach (var gc in gcs)
                {
                    if (gc.GommeTeamMemberRoleId == null) continue;
                    var guild = _client.Guilds.First(sg => sg.Id == gc.GuildId);
                    var gommeTeamRole = guild.GetRole(gc.GommeTeamMemberRoleId.Value);
                    if (gommeTeamRole == null) continue;
                    var verifiedUsers = uow.VerifiedUsers.GetVerifiedUsers(gc.GuildId).Select(vu => (ForumUserId: vu.ForumUserId, User: guild.GetUser(vu.UserId))).ToList();
                    foreach (var (_, user) in verifiedUsers.Where(a => a.User.Roles.Any(r => r.Id == gc.GommeTeamMemberRoleId) && !staffIds.Contains(a.ForumUserId)))
                    {
                        await user.RemoveRoleAsync(gommeTeamRole).ConfigureAwait(false);
                    }

                    foreach (var (_, user) in verifiedUsers.Where(a => staffIds.Contains(a.ForumUserId)))
                    {
                        await user.AddRoleAsync(gommeTeamRole).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}