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
        private const int GommeTeamMemberCheckRepeatDelay = 5 * 60 * 1000;

        public GommeTeamSyncService(DbService db, ForumService fs, DiscordSocketClient client)
        {
            _db = db;
            _fs = fs;
            _client = client;

            _timerTask = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await CheckGommeTeamMembers();
                    }
                    catch { /* ignored */ }
                    await Task.Delay(GommeTeamMemberCheckRepeatDelay);
                }
            });
        }

        public async Task CheckGommeTeamMembers()
        {
            if (_fs.Forum == null) return;
            var staffIds = (await _fs.Forum.GetMembersList(MembersListType.Staff)).Select(ui => ui.Id).ToList();

            using (var uow = _db.UnitOfWork)
            {
                var guilds = uow.GuildConfigs.GetAllGuildConfigs(_client.Guilds.Select(sg => sg.Id).ToList()).Select(gc => (gc, guild: _client.GetGuild(gc.GuildId))).ToList();
                foreach (var (gc, guild) in guilds)
                {
                    if (gc.GommeTeamMemberRoleId == null) continue;
                    var gommeTeamRole = guild.GetRole(gc.GommeTeamMemberRoleId.Value);
                    if (gommeTeamRole == null) continue;
                    var vipRole = gc.VipRoleId.HasValue ? guild.GetRole(gc.VipRoleId.Value) : null;
                    var verifiedUsers = uow.VerifiedUsers.GetVerifiedUsers(gc.GuildId)
                        .Select(vu => (vu.ForumUserId, User: guild.GetUser(vu.UserId)))
                        .Where(a => a.User != null)
                        .ToList();

                    foreach (var (_, user) in verifiedUsers.Where(a => a.User.Roles.Any(r => r.Id == gommeTeamRole.Id) && !staffIds.Contains(a.ForumUserId)))
                    {
                        await user.RemoveRoleAsync(gommeTeamRole).ConfigureAwait(false);
                        if (vipRole != null && user.Roles.All(r => r.Id != vipRole.Id)) await user.AddRoleAsync(vipRole).ConfigureAwait(false);
                    }

                    foreach (var (_, user) in verifiedUsers.Where(a => a.User.Roles.All(r => r.Id != gommeTeamRole.Id) && staffIds.Contains(a.ForumUserId)))
                    {
                        await user.AddRoleAsync(gommeTeamRole).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}