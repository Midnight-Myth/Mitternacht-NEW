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

            LevelModel lm;
            int totalRanks, rank;
            using (var uow = _db.UnitOfWork)
            {
                lm = uow.LevelModel.GetOrCreate(userId);
                totalRanks = uow.LevelModel.GetAll().Count(m => m.TotalXP > 0);
                rank = uow.LevelModel.GetAll().OrderByDescending(p => p.TotalXP).ToList().IndexOf(lm) + 1;
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            if (userId == Context.User.Id) {
                await Context.Channel.SendMessageAsync(GetText("rank_self", Context.User.Mention, lm.Level, lm.CurrentXP, LevelModelRepository.GetXpToNextLevel(lm.Level), lm.TotalXP, lm.TotalXP > 0 ? rank.ToString() : "-", totalRanks)).ConfigureAwait(false);
            }
            else {
                var user = await Context.Guild.GetUserAsync(userId).ConfigureAwait(false);
                var namestring = user?.Nickname ?? (user?.Username ?? userId.ToString());
                await Context.Channel.SendMessageAsync(GetText("rank_other", Context.User.Mention, namestring, lm.Level, lm.CurrentXP, LevelModelRepository.GetXpToNextLevel(lm.Level), lm.TotalXP, lm.TotalXP > 0 ? rank.ToString() : "-", totalRanks)).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Ranks(int count, int position)
        {
            const int elementsPerList = 20;

            List<LevelModel> levelModels;
            using (var uow = _db.UnitOfWork)
            {
                levelModels = uow.LevelModel.GetAll().Where(p => p.TotalXP > 0).OrderByDescending(p => p.TotalXP).Skip(position-1 <= 0 ? 0 : position-1).Take(count).ToList();
            }

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
                    sb.Append("\n" + GetText("ranks_list_row", $"{position + levelModels.IndexOf(lm),3}", $"{user?.ToString() ?? lm.UserId.ToString(),-37}", $"{lm.Level,3}", $"{lm.CurrentXP,6}", $"{LevelModelRepository.GetXpToNextLevel(lm.Level),6}", $"{lm.TotalXP,8}"));
                }
                sb.Append("```");
                rankStrings.Add(sb.ToString());
                sb.Clear();
            }

            var channel = count <= 20 ? Context.Channel : await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);
            foreach (var s in rankStrings)
            {
                await channel.SendMessageAsync(s).ConfigureAwait(false);
                Thread.Sleep(250);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Ranks(int count = 20) 
            => await Ranks(count, 1);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task AddXp(int xp, [Remainder]IUser user = null)
        {
            user = user ?? Context.User;
            using (var uow = _db.UnitOfWork)
            {
                uow.LevelModel.TryAddXp(user.Id, xp, false);
                await ConfirmLocalized("addxp", xp, user.ToString()).ConfigureAwait(false);
                var level = uow.LevelModel.CalculateLevel(user.Id);
                await Service.SendLevelChangedMessage(level, user, Context.Channel).ConfigureAwait(false);
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
                uow.LevelModel.SetXp(user.Id, xp, false);
                await ConfirmLocalized("setxp", user.ToString(), xp).ConfigureAwait(false);
                var level = uow.LevelModel.CalculateLevel(user.Id);
                await Service.SendLevelChangedMessage(level, user, Context.Channel).ConfigureAwait(false);
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
                uow.LevelModel.SetXp(userId, xp, false);
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
                uow.LevelModel.TryAddXp(user.Id, (int)moneyToSpend * 5, false);
                if (user == Context.User) await ReplyConfirmLocalized("ttxp_turned_self", moneyToSpend, CurrencySign, moneyToSpend * 5).ConfigureAwait(false);
                else await ReplyConfirmLocalized("ttxp_turned_other", user.ToString(), moneyToSpend, CurrencySign, moneyToSpend * 5).ConfigureAwait(false);
                var level = uow.LevelModel.CalculateLevel(user.Id);
                await Service.SendLevelChangedMessage(level, user, Context.Channel).ConfigureAwait(false);
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
            var pagecount = roleLevelBindings.Count / elementsPerPage;
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
                    embed.AddInlineField($"#{elementsPerPage * p + rlbs.IndexOf(rlb)} - {rolename}", rlb.MinimumLevel);
                }
                return embed;
            }, pagecount).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task MsgXpRestrictionAdd(ITextChannel channel) {
            using (var uow = _db.UnitOfWork) {
                var success = await uow.MessageXpBlacklist.CreateRestrictionAsync(channel).ConfigureAwait(false);
                if (success)
                    await ConfirmLocalized("msgxpr_add_success", channel.Mention).ConfigureAwait(false);
                else
                    await ErrorLocalized("msgxpr_add_fail", channel.Mention).ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task MsgXpRestrictionRemove(ITextChannel channel) {
            using (var uow = _db.UnitOfWork) {
                var success = await uow.MessageXpBlacklist.RemoveRestrictionAsync(channel).ConfigureAwait(false);
                if (success)
                    await ConfirmLocalized("msgxpr_remove_success", channel.Mention).ConfigureAwait(false);
                else
                    await ErrorLocalized("msgxpr_remove_fail", channel.Mention).ConfigureAwait(false);
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
    }
}
