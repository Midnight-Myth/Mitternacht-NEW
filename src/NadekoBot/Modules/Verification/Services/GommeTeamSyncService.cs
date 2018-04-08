using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using GommeHDnetForumAPI.DataModels;
using Mitternacht.Modules.Forum.Services;
using Mitternacht.Services;
using NLog;

namespace Mitternacht.Modules.Verification.Services
{
    public class GommeTeamSyncService : INService
    {
        private readonly DbService _db;
        private readonly ForumService _fs;
        private readonly DiscordSocketClient _client;
        private readonly Logger _log;

        private Task _timerTask;
        private const int GommeTeamMemberCheckRepeatDelay = 30 * 1000;
        public bool EnableLogging = false;

        public GommeTeamSyncService(DbService db, ForumService fs, DiscordSocketClient client)
        {
            _db = db;
            _fs = fs;
            _client = client;
            _log = LogManager.GetCurrentClassLogger();

            _timerTask = Task.Run(async () =>
            {
                while (true)
                {
                    if (EnableLogging) _log.Info($"Waiting {GommeTeamMemberCheckRepeatDelay}s...");
                    await Task.Delay(GommeTeamMemberCheckRepeatDelay);
                    if (EnableLogging) _log.Info("Executing CheckGommeTeamMembers");
                    try
                    {
                        await CheckGommeTeamMembers();
                    }
                    catch (Exception e)
                    {
                        _log.Info($"CheckGommeTeamMembers failed: {e.Message}\n{e.StackTrace}");
                    }
                }
            });
        }

        private async Task CheckGommeTeamMembers()
        {
            if (_fs.Forum == null) return;
            var staffIds = (await _fs.Forum.GetMembersList(MembersListType.Staff).ConfigureAwait(false)).Select(ui => ui.Id).ToList();

            if (EnableLogging) _log.Info($"staff: {staffIds.Count}");

            using (var uow = _db.UnitOfWork)
            {
                var guilds = uow.GuildConfigs.GetAllGuildConfigs(_client.Guilds.Select(sg => sg.Id).ToList()).Select(gc => (gc: gc, guild: _client.GetGuild(gc.GuildId))).ToList();

                if (EnableLogging) _log.Info($"guilds: {string.Join(", ", guilds.Select(g => g.guild.Name))}");

                foreach (var (gc, guild) in guilds)
                {
                    if (EnableLogging) _log.Info($"guild {guild.Name} {gc.GommeTeamMemberRoleId}");

                    if (gc.GommeTeamMemberRoleId == null) continue;
                    var gommeTeamRole = guild.GetRole(gc.GommeTeamMemberRoleId.Value);

                    if (EnableLogging) _log.Info($"gtr: {gommeTeamRole?.Name}");

                    if (gommeTeamRole == null) continue;
                    var verifiedUsers = uow.VerifiedUsers.GetVerifiedUsers(gc.GuildId).Select(vu => (ForumUserId: vu.ForumUserId, User: guild.GetUser(vu.UserId))).Where(a => a.User != null).ToList();
                    foreach (var (_, user) in verifiedUsers.Where(a => a.User.Roles.Any(r => r.Id == gommeTeamRole.Id) && !staffIds.Contains(a.ForumUserId)))
                    {
                        await user.RemoveRoleAsync(gommeTeamRole).ConfigureAwait(false);
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