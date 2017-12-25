using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Level.Common;
using Mitternacht.Modules.Level.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Database.Repositories.Impl;

namespace Mitternacht.Modules.Level
{
    public class Level : NadekoTopLevelModule<LevelService>
    {
        private readonly IBotConfigProvider _bc;
        private readonly DbService _db;
        private readonly IBotCredentials _creds;

        private string CurrencySign => _bc.BotConfig.CurrencySign;

        public Level(IBotConfigProvider bc, IBotCredentials creds, DbService db) {
            _bc = bc;
            _db = db;
            _creds = creds;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Rank([Remainder] IUser user = null)
            => await Rank(user?.Id ?? 0);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Rank(ulong userId = 0)
        {
            userId = userId != 0 ? userId : Context.User.Id;
            
            int totalRanks, rank, totalXp, level, currentXp;
            using (var uow = _db.UnitOfWork) {
                var lm = uow.LevelModel.Get(Context.Guild.Id, userId);
                totalXp = uow.LevelModel.GetTotalXp(Context.Guild.Id, userId);
                level = uow.LevelModel.GetLevel(Context.Guild.Id, userId);
                currentXp = uow.LevelModel.GetCurrentXp(Context.Guild.Id, userId);
                totalRanks = uow.LevelModel.GetAll().Count(m => m.TotalXP > 0 && m.GuildId == Context.Guild.Id);
                rank = lm == null ? -1 : uow.LevelModel.GetAll().Where(p => p.GuildId == Context.Guild.Id).OrderByDescending(p => p.TotalXP).ToList().IndexOf(lm) + 1;
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            if (userId == Context.User.Id) {
                await Context.Channel.SendMessageAsync(GetText("rank_self", Context.User.Mention, level, currentXp, LevelModelRepository.GetXpToNextLevel(level), totalXp, totalXp > 0 ? rank.ToString() : "-", totalRanks)).ConfigureAwait(false);
            }
            else {
                var user = await Context.Guild.GetUserAsync(userId).ConfigureAwait(false);
                var namestring = user?.Nickname ?? (user?.Username ?? userId.ToString());
                await Context.Channel.SendMessageAsync(GetText("rank_other", Context.User.Mention, namestring, level, currentXp, LevelModelRepository.GetXpToNextLevel(level), totalXp, totalXp > 0 ? rank.ToString() : "-", totalRanks)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Ranks(int count, int position)
        {
            const int elementsPerList = 20;
            using (var uow = _db.UnitOfWork) {
                var levelModels = uow.LevelModel.GetAll().Where(p => p.TotalXP != 0 && p.GuildId == Context.Guild.Id).OrderByDescending(p => p.TotalXP).Skip(position - 1 <= 0 ? 0 : position - 1).Take(count).ToList();

                if (!levelModels.Any()) return;

                var groupedLevelModels = levelModels.GroupBy(lm => (int) Math.Floor(levelModels.IndexOf(lm) * 1d / elementsPerList));
                var rankStrings = new List<string>();
                var sb = new StringBuilder();
                sb.AppendLine(GetText("ranks_header"));
                foreach (var glm in groupedLevelModels) {
                    var listNumber = glm.Key + 1;
                    if (!glm.Any()) continue;
                    sb.Append($"```{GetText("ranks_list_header", listNumber)}");
                    foreach (var lm in glm) {
                        var user = await Context.Guild.GetUserAsync(lm.UserId).ConfigureAwait(false);
                        var level = uow.LevelModel.GetLevel(lm.GuildId, lm.UserId);
                        var currentXp = uow.LevelModel.GetCurrentXp(lm.GuildId, lm.UserId);
                        sb.Append("\n" + GetText("ranks_list_row", $"{position + levelModels.IndexOf(lm),3}", $"{user?.ToString() ?? lm.UserId.ToString(),-37}", $"{level,3}", $"{currentXp,6}", $"{LevelModelRepository.GetXpToNextLevel(level),6}", $"{lm.TotalXP,8}"));
                    }
                    sb.Append("```");
                    rankStrings.Add(sb.ToString());
                    sb.Clear();
                }
                
                var channel = count <= 20 ? Context.Channel : await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                foreach (var s in rankStrings) {
                    await channel.SendMessageAsync(s).ConfigureAwait(false);
                    Thread.Sleep(250);
                }
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Ranks(int count = 20) 
            => await Ranks(count, 1);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task AddXp(int xp, [Remainder]IUser user = null) {
            user = user ?? Context.User;

            using (var uow = _db.UnitOfWork)
            {
                uow.LevelModel.AddXp(Context.Guild.Id, user.Id, xp, Context.Channel.Id);
                await ConfirmLocalized("addxp", xp, user.ToString()).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [OwnerOnly]
        public async Task SetXp(int xp, [Remainder] IUser user = null)
        {
            user = user ?? Context.User;
            using (var uow = _db.UnitOfWork)
            {
                uow.LevelModel.SetXp(Context.Guild.Id, user.Id, xp, Context.Channel.Id);
                await ConfirmLocalized("setxp", user.ToString(), xp).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [OwnerOnly]
        public async Task SetXp(int xp, ulong userId) {
            var user = await Context.Guild.GetUserAsync(userId);
            if (user != null) {
                await SetXp(xp, user).ConfigureAwait(false);
                return;
            }
            using (var uow = _db.UnitOfWork)
            {
                uow.LevelModel.SetXp(Context.Guild.Id, userId, xp, Context.Channel.Id);
                await ConfirmLocalized("setxp", userId, xp).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task TurnToXp(long moneyToSpend, [Remainder] IUser user = null)
        {
            user = user != null && _creds.IsOwner(Context.User) ? user : Context.User;
            if(moneyToSpend < 0) {
                await ReplyErrorLocalized("ttxp_error_negative_value").ConfigureAwait(false);
                return;
            }
            if(moneyToSpend == 0) {
                await ReplyErrorLocalized("ttxp_error_zero_value", CurrencySign).ConfigureAwait(false);
                return;
            }
            using (var uow = _db.UnitOfWork)
            {
                if(!uow.Currency.TryUpdateState(user.Id, -moneyToSpend)) {
                    if (user == Context.User) await ReplyErrorLocalized("ttxp_error_no_money_self").ConfigureAwait(false);
                    else await ReplyErrorLocalized("ttxp_error_no_money_other", user.ToString()).ConfigureAwait(false);
                    return;
                }
                var xp = (int)(moneyToSpend * uow.GuildConfigs.For(Context.Guild.Id, set => set).TurnToXpMultiplier);
                uow.LevelModel.AddXp(Context.Guild.Id, user.Id, xp, Context.Channel.Id);
                if (user == Context.User) await ReplyConfirmLocalized("ttxp_turned_self", moneyToSpend, CurrencySign, xp).ConfigureAwait(false);
                else await ReplyConfirmLocalized("ttxp_turned_other", user.ToString(), moneyToSpend, CurrencySign, xp).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task SetRoleLevelBinding(IRole role, int minlevel) {
            if (minlevel < 0) {
                await ErrorLocalized("rlb_set_minlevel").ConfigureAwait(false);
                return;
            }
            using (var uow = _db.UnitOfWork)
            {
                uow.RoleLevelBinding.SetBinding(role.Id, minlevel);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await ConfirmLocalized("rlb_set", role.Name, minlevel);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task RemoveRoleLevelBinding(IRole role)
        {
            bool wasRemoved;
            using (var uow = _db.UnitOfWork)
            {
                wasRemoved = uow.RoleLevelBinding.Remove(role.Id);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            if (wasRemoved) await ConfirmLocalized("rlb_removed", role.Name).ConfigureAwait(false);
            else await ErrorLocalized("rlb_already_independent", role.Name).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task RoleLevelBindings(int page = 1)
        {
            const int elementsPerPage = 9;

            List<RoleLevelBinding> roleLevelBindings;
            using (var uow = _db.UnitOfWork)
            {
                roleLevelBindings = uow.RoleLevelBinding.GetAll().OrderByDescending(r => r.MinimumLevel).ToList();
            }

            if (!roleLevelBindings.Any()) {
                await ReplyErrorLocalized("rlb_none").ConfigureAwait(false);
                return;
            }
            var pagecount = (int)Math.Ceiling(roleLevelBindings.Count*1d / elementsPerPage);
            if (page > pagecount) {
                await ReplyErrorLocalized("rlb_page_too_high").ConfigureAwait(false);
                return;
            }
            if (page < 1) page = 1;

            await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, p => {
                var embed = new EmbedBuilder()
                    .WithTitle(GetText("rlb_title"));
                var rlbs = roleLevelBindings.Skip(elementsPerPage * p).Take(elementsPerPage).ToList();
                foreach (var rlb in rlbs) {
                    var rolename = Context.Guild.GetRole(rlb.RoleId)?.Name ?? rlb.RoleId.ToString();
                    embed.AddInlineField($"#{elementsPerPage * p + rlbs.IndexOf(rlb) + 1} - {rolename}", rlb.MinimumLevel);
                }
                return embed;
            }, pagecount-1).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task MsgXpRestrictionAdd(ITextChannel channel) {
            using (var uow = _db.UnitOfWork) {
                var success = uow.MessageXpBlacklist.CreateRestriction(channel);
                if (success)
                    await ConfirmLocalized("msgxpr_add_success", channel.Mention).ConfigureAwait(false);
                else
                    await ErrorLocalized("msgxpr_add_fail", channel.Mention).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task MsgXpRestrictionRemove(ITextChannel channel) {
            using (var uow = _db.UnitOfWork) {
                var success = uow.MessageXpBlacklist.RemoveRestriction(channel);
                if (success)
                    await ConfirmLocalized("msgxpr_remove_success", channel.Mention).ConfigureAwait(false);
                else
                    await ErrorLocalized("msgxpr_remove_fail", channel.Mention).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MsgXpRestrictions() {
            using (var uow = _db.UnitOfWork) {
                if (!uow.MessageXpBlacklist.GetAll().Any()) await ErrorLocalized("msgxpr_none").ConfigureAwait(false);
                else
                    await Context.Channel.SendConfirmAsync(GetText("msgxpr_title"),
                            uow.MessageXpBlacklist
                                .GetAll()
                                .OrderByDescending(m => m.ChannelId)
                                .Aggregate("", (s, m) => $"{s}{MentionUtils.MentionChannel(m.ChannelId)}, ", s => s.Substring(0, s.Length - 2)))
                        .ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task LevelGuildData(LevelGuildData data, double value) {
            var previous = 0d;
            using (var uow = _db.UnitOfWork) {
                var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                switch (data)
                {
                    case Common.LevelGuildData.TurnToXpMultiplier:
                        previous = gc.TurnToXpMultiplier;
                        gc.TurnToXpMultiplier = value;
                        break;
                    case Common.LevelGuildData.MessageXpCharCountMin:
                        previous = gc.MessageXpCharCountMin;
                        gc.MessageXpCharCountMin = (int) value;
                        break;
                    case Common.LevelGuildData.MessageXpCharCountMax:
                        previous = gc.MessageXpCharCountMax;
                        gc.MessageXpCharCountMax = (int) value;
                        break;
                    case Common.LevelGuildData.MessageXpTimeDifference:
                        previous = gc.MessageXpTimeDifference;
                        gc.MessageXpTimeDifference = value;
                        break;
                }
                uow.GuildConfigs.Update(gc);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            var round = data == Common.LevelGuildData.MessageXpCharCountMax || data == Common.LevelGuildData.MessageXpCharCountMin;
            await ConfirmLocalized("levelguilddata_changed", data.ToString(), round ? (int) previous : previous, round ? (int) value : value).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task LevelGuildData(LevelGuildData data) {
            var value = 0d;
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                switch (data)
                {
                    case Common.LevelGuildData.TurnToXpMultiplier:
                        value = gc.TurnToXpMultiplier;
                        break;
                    case Common.LevelGuildData.MessageXpCharCountMin:
                        value = gc.MessageXpCharCountMin;
                        break;
                    case Common.LevelGuildData.MessageXpCharCountMax:
                        value = gc.MessageXpCharCountMax;
                        break;
                    case Common.LevelGuildData.MessageXpTimeDifference:
                        value = gc.MessageXpTimeDifference;
                        break;
                }
            }
            await ConfirmLocalized("levelguilddata", data.ToString(), value).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task LevelGuildDataChoices() {
            await Context.Channel.SendConfirmAsync(string.Join(", ", Enum.GetNames(typeof(LevelGuildData)))).ConfigureAwait(false);
        }
    }
}
