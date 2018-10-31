using Discord.WebSocket;
using GommeHDnetForumAPI.DataModels;
using GommeHDnetForumAPI.DataModels.Collections;
using GommeHDnetForumAPI.DataModels.Entities;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Forum.Services
{
    public class TeamUpdateService : INService
    {
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;
        private readonly ForumService _fs;
        private readonly StringService _ss;
        private UserCollection _staff;

        private readonly Task _teamUpdateTask;

        public event Func<UserInfo[], Task> TeamRankAdded = ui => Task.CompletedTask;
        public event Func<UserInfo[], Task> TeamRankChanged = ui => Task.CompletedTask;
        public event Func<UserInfo[], Task> TeamRankRemoved = ui => Task.CompletedTask;

        public TeamUpdateService(DiscordSocketClient client, DbService db, ForumService fs, StringService ss)
        {
            _client = client;
            _db = db;
            _fs = fs;
            _ss = ss;

            _staff = new UserCollection();

            _teamUpdateTask = Task.Run(async () =>
            {
                while (true)
                {
                    await DoTeamUpdate();
                    await Task.Delay(30 * 1000);
                }
            });
        }

        public async Task DoTeamUpdate()
        {
            if (_fs.Forum == null) return;
            var staff = await _fs.Forum.GetMembersList(MembersListType.Staff).ConfigureAwait(false);
            var rankAdded = staff.Where(ui => staff.All(ui2 => ui2.Id != ui.Id)).ToArray();
            var rankChanged = _staff.Where(ui => staff.Any(ui2 => ui2.Id == ui.Id)).ToArray();
            var rankRemoved = _staff.Where(ui => staff.All(ui2 => ui2.Id != ui.Id)).ToArray();

            await TeamRankAdded.Invoke(rankAdded).ConfigureAwait(false);
            await TeamRankChanged.Invoke(rankChanged).ConfigureAwait(false);
            await TeamRankRemoved.Invoke(rankRemoved).ConfigureAwait(false);

            using (var uow = _db.UnitOfWork)
            {
                foreach (var (guildId, tuChId, tuMPrefix) in uow.GuildConfigs.GetAll().Where(gc => gc.TeamUpdateChannelId.HasValue).Select(gc => (GuildId: gc.GuildId, TeamUpdateChannelId: gc.TeamUpdateChannelId.Value, TeamUpdateMessagePrefix: gc.TeamUpdateMessagePrefix)))
                {
                    var guild = _client.GetGuild(guildId);
                    if (guild == null) continue;
                    var tuCh = guild.GetTextChannel(tuChId);
                    if (tuCh == null) continue;
                    var prefix = string.IsNullOrWhiteSpace(tuMPrefix) ? "" : tuMPrefix;

                }
            }

            _staff = staff;
        }

        private string GetText(string key, ulong? guildId, params string[] replacements)
            => _ss.GetText(key, guildId, "forum", replacements);
    }
}
