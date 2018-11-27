using Discord.WebSocket;
using GommeHDnetForumAPI.DataModels;
using GommeHDnetForumAPI.DataModels.Collections;
using GommeHDnetForumAPI.DataModels.Entities;
using Mitternacht.Modules.Forum.Common;
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
        public event Func<RankUpdateItem[], Task> TeamRankChanged = ui => Task.CompletedTask;
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
            var rankAdded = staff.Where(uiNew => _staff.All(uiOld => uiOld.Id != uiNew.Id)).ToArray();
            var rankChanged = _staff.Where(uiOld => staff.Any(uiNew => uiNew.Id == uiOld.Id)).Select(uiOld => new RankUpdateItem(uiOld, staff.First(uiNew => uiNew.Id == uiOld.Id))).ToArray();
            var rankRemoved = _staff.Where(uiOld => staff.All(uiNew => uiNew.Id != uiOld.Id)).ToArray();

            await TeamRankAdded.Invoke(rankAdded).ConfigureAwait(false);
            await TeamRankChanged.Invoke(rankChanged).ConfigureAwait(false);
            await TeamRankRemoved.Invoke(rankRemoved).ConfigureAwait(false);

            using (var uow = _db.UnitOfWork)
            {
                foreach (var (guildId, tuChId, tuMPrefix) in uow.GuildConfigs.GetAll().Where(gc => gc.TeamUpdateChannelId.HasValue).Select(gc => (GuildId: gc.GuildId, TeamUpdateChannelId: gc.TeamUpdateChannelId.Value, TeamUpdateMessagePrefix: gc.TeamUpdateMessagePrefix)))
                {
                    var guild = _client.GetGuild(guildId);
                    var tuCh = guild?.GetTextChannel(tuChId);
                    if (tuCh == null) continue;
                    var prefix = string.IsNullOrWhiteSpace(tuMPrefix) ? "" : $"{tuMPrefix.Trim()} ";
                    var roles = uow.TeamUpdateRank.GetGuildRanks(guildId);
                    if (roles.Count == 0) continue;

                    //rankAdded.GroupBy()
                }
            }

            _staff = staff;
        }

        private string GetText(string key, ulong? guildId, params object[] replacements)
            => _ss.GetText(key, guildId, "forum", replacements);
    }
}
