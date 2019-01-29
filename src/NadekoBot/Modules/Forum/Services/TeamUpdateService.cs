using Discord.WebSocket;
using GommeHDnetForumAPI.DataModels;
using GommeHDnetForumAPI.DataModels.Collections;
using GommeHDnetForumAPI.DataModels.Entities;
using Mitternacht.Modules.Forum.Common;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private UserCollection _staff = new UserCollection();

        private readonly Task _teamUpdateTask;

        public event Func<SocketTextChannel, UserInfo[], Task> TeamRankAdded = (g, ui) => Task.CompletedTask;
        public event Func<SocketTextChannel, RankUpdateItem[], Task> TeamRankChanged = (g, rui) => Task.CompletedTask;
        public event Func<SocketTextChannel, UserInfo[], Task> TeamRankRemoved = (g, rui) => Task.CompletedTask;

        private const int _teamUpdateInterval = 30 * 1000;

        public TeamUpdateService(DiscordSocketClient client, DbService db, ForumService fs, StringService ss)
        {
            _client = client;
            _db = db;
            _fs = fs;
            _ss = ss;

            _teamUpdateTask = Task.Run(async () =>
            {
                while (_fs.Forum == null) await Task.Delay(500);
                _staff = await _fs.Forum.GetMembersList(MembersListType.Staff);

                var log = LogManager.GetCurrentClassLogger();

                while (true)
                {
                    try
                    {
                        await DoTeamUpdate();
                    }
                    catch (Exception e)
                    {
                        log.Warn(e, CultureInfo.CurrentCulture, "Team updating failed!");
                    }
                    await Task.Delay(_teamUpdateInterval);
                }
            });

            TeamRankAdded += OnRankAdded;
            TeamRankChanged += OnRankChanged;
            TeamRankRemoved += OnRankRemoved;
        }

        public async Task DoTeamUpdate()
        {
            if (_fs.Forum == null) return;
            var staff = await _fs.Forum.GetMembersList(MembersListType.Staff).ConfigureAwait(false);
            var rankAdded = staff.Where(uiNew => _staff.All(uiOld => uiOld.Id != uiNew.Id)).ToArray();
            var rankChanged = _staff.Where(uiOld => staff.Any(uiNew => uiNew.Id == uiOld.Id && !string.Equals(uiNew.UserTitle, uiOld.UserTitle, StringComparison.OrdinalIgnoreCase))).Select(uiOld => new RankUpdateItem(uiOld, staff.First(uiNew => uiNew.Id == uiOld.Id))).ToArray();
            var rankRemoved = _staff.Where(uiOld => staff.All(uiNew => uiNew.Id != uiOld.Id)).ToArray();

            List<GuildConfig> guildConfigs;
            List<IGrouping<ulong, TeamUpdateRank>> teamUpdateRanks;

            using (var uow = _db.UnitOfWork)
            {
                guildConfigs = uow.GuildConfigs.GetAllGuildConfigs(_client.Guilds.Select(g => g.Id).ToList()).Where(gc => gc.TeamUpdateChannelId.HasValue).ToList();
                teamUpdateRanks = uow.TeamUpdateRank.GetAll().GroupBy(tur => tur.GuildId).Where(turgroup => guildConfigs.Any(gc => gc.GuildId == turgroup.Key)).ToList();
            }

            foreach (var gc in guildConfigs)
            {
                var guild = _client.GetGuild(gc.GuildId);
                var tuCh = guild?.GetTextChannel(gc.TeamUpdateChannelId.Value);
                if (tuCh == null) continue;
                var roles = teamUpdateRanks.FirstOrDefault(turgroup => turgroup.Key == gc.GuildId)?.ToArray();
                if (roles is null || roles.Length == 0) continue;

                await TeamRankAdded.Invoke(tuCh, rankAdded).ConfigureAwait(false);
                await TeamRankChanged.Invoke(tuCh, rankChanged).ConfigureAwait(false);
                await TeamRankRemoved.Invoke(tuCh, rankRemoved).ConfigureAwait(false);
            }

            _staff = staff;
        }


        #region TeamUpdate Event Handler

        private async Task OnRankChanged(SocketTextChannel channel, RankUpdateItem[] rankUpdates)
        {
            var roles = GetForumRankUpdateRoles(channel.Guild.Id);
            if (roles.Length == 0) return;
            var userGroup = rankUpdates.Where(rui => roles.Any(r => r.Equals(rui.NewRank, StringComparison.OrdinalIgnoreCase) || r.Equals(rui.OldRank, StringComparison.OrdinalIgnoreCase)))
                                       .GroupBy<RankUpdateItem, (string OldRank, string NewRank)>(rui => (rui.OldRank, rui.NewRank), new StringValueTupleComparer()).ToList();
            var prefix = GetForumRankUpdateMessagePrefix(channel.Guild.Id);

            using (var uow = _db.UnitOfWork)
            {
                foreach (var rank in userGroup)
                {
                    var key = $"teamupdate_changed_{(rank.Count() == 1 ? "single" : "multi")}";
                    var userString = string.Join(", ", rank.Select(rui =>
                    {
                        var uid = uow.VerifiedUsers.GetVerifiedUserId(channel.Guild.Id, rui.NewUserInfo.Id);
                        if (!uid.HasValue) return rui.NewUserInfo.Username;
                        var user = channel.Guild.GetUser(uid.Value);
                        if (user is null) return rui.NewUserInfo.Username;
                        return user.Mention;
                    }));
                    await channel.SendMessageAsync(prefix + GetText(key, channel.Guild.Id, userString, rank.Key.OldRank, rank.Key.NewRank)).ConfigureAwait(false);
                }
            }
        }

        private async Task OnRankAdded(SocketTextChannel channel, UserInfo[] userInfos)
            => await RankAddedRemovedUpdate(channel, userInfos, "added");

        private async Task OnRankRemoved(SocketTextChannel channel, UserInfo[] userInfos)
            => await RankAddedRemovedUpdate(channel, userInfos, "removed");

        private async Task RankAddedRemovedUpdate(SocketTextChannel channel, UserInfo[] userInfos, string keypart)
        {
            var roles = GetForumRankUpdateRoles(channel.Guild.Id);
            if (roles.Length == 0) return;
            var userGroup = userInfos.Where(ui => roles.Any(r => r.Equals(ui.UserTitle, StringComparison.OrdinalIgnoreCase))).GroupBy(ui => ui.UserTitle, StringComparer.OrdinalIgnoreCase).ToList();
            var prefix = GetForumRankUpdateMessagePrefix(channel.Guild.Id);

            using (var uow = _db.UnitOfWork)
            {
                foreach (var rank in userGroup)
                {
                    var key = $"teamupdate_{keypart}_{(rank.Count() == 1 ? "single" : "multi")}";
                    var userString = string.Join(", ", rank.Select(ui =>
                    {
                        var uid = uow.VerifiedUsers.GetVerifiedUserId(channel.Guild.Id, ui.Id);
                        if (!uid.HasValue) return ui.Username;
                        var user = channel.Guild.GetUser(uid.Value);
                        if (user is null) return ui.Username;
                        return user.Mention;
                    }));
                    await channel.SendMessageAsync(prefix + GetText(key, channel.Guild.Id, userString, rank.Key)).ConfigureAwait(false);
                }
            }
        }

        private string[] GetForumRankUpdateRoles(ulong guildId)
        {
            using(var uow = _db.UnitOfWork)
            {
                return uow.TeamUpdateRank.GetGuildRanks(guildId).ToArray();
            }
        }

        private string GetForumRankUpdateMessagePrefix(ulong guildId)
        {
            using (var uow = _db.UnitOfWork)
            {
                var prefix = uow.GuildConfigs.For(guildId).TeamUpdateMessagePrefix;
                return string.IsNullOrWhiteSpace(prefix) ? "" : $"{prefix.Trim()} ";
            }
        }

        #endregion


        public bool AnnouncesTeamUpdates(ulong guildId)
        {
            using(var uow = _db.UnitOfWork)
            {
                return uow.GuildConfigs.For(guildId).TeamUpdateChannelId.HasValue;
            }
        }

        private string GetText(string key, ulong? guildId, params object[] replacements)
            => _ss.GetText(key, guildId, "forum", replacements);


        private class StringValueTupleComparer : IEqualityComparer<(string s1, string s2)>
        {
            public bool Equals((string s1, string s2) x, (string s1, string s2) y) 
                => string.Equals(x.s1, y.s1, StringComparison.OrdinalIgnoreCase) && string.Equals(x.s2, y.s2, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string s1, string s2) obj) 
                => obj.s1.GetHashCode() + obj.s2.GetHashCode();
        }
    }
}
