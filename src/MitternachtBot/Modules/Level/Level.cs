﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Level.Services;
using Mitternacht.Services;
using Mitternacht.Database;
using Mitternacht.Database.Models;

namespace Mitternacht.Modules.Level {
	[Group]
	public partial class Level : MitternachtTopLevelModule<LevelService> {
		private readonly IBotConfigProvider _bc;
		private readonly IUnitOfWork uow;
		private readonly IBotCredentials    _creds;

		private string CurrencySign => _bc.BotConfig.CurrencySign;

		public Level(IBotConfigProvider bc, IBotCredentials creds, IUnitOfWork uow) {
			_bc    = bc;
			this.uow    = uow;
			_creds = creds;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Rank([Remainder] IUser user = null)
			=> await Rank(user?.Id ?? 0);

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Rank(ulong userId = 0) {
			userId        = userId != 0 ? userId : Context.User.Id;

			var lm        = uow.LevelModel.Get(Context.Guild.Id, userId);

			if(lm != null) {
				var guildUserIds = (await Context.Guild.GetUsersAsync()).Select(gu => gu.Id).ToArray();
				var levelModels  = uow.LevelModel.ForGuildOrderedByTotalXP(Context.Guild.Id, guildUserIds).ToList();

				var rank         = levelModels.IndexOf(lm) + 1;
				var totalRanks   = levelModels.Count();
				var rankString   = lm.TotalXP > 0 && rank > 0 ? rank.ToString() : "-";

				if(userId == Context.User.Id) {
					await Context.Channel.SendMessageAsync(GetText("rank_self", Context.User.Mention, lm.Level, lm.XP, LevelModel.GetXpToNextLevel(lm.Level), lm.TotalXP, rankString, totalRanks)).ConfigureAwait(false);
				} else {
					var user       = await Context.Guild.GetUserAsync(userId).ConfigureAwait(false);
					var namestring = user?.ToString() ?? uow.UsernameHistory.GetLastUsername(userId) ?? userId.ToString();
					await Context.Channel.SendMessageAsync(GetText("rank_other", Context.User.Mention, namestring, lm.Level, lm.XP, LevelModel.GetXpToNextLevel(lm.Level), lm.TotalXP, rankString, totalRanks)).ConfigureAwait(false);
				}
			} else {
				await ReplyErrorLocalized("rank_not_found").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Ranks(int count = 20, int position = 1) {
			
			var guildUserIds = (await Context.Guild.GetUsersAsync()).Select(gu => gu.Id).ToArray();
			
			var levelModels = uow.LevelModel
				.ForGuildOrderedByTotalXP(Context.Guild.Id, guildUserIds)
				.Skip(position <= 1 ? 0 : position - 1)
				.Take(count)
				.ToList();

			var channel = count <= 20
				? Context.Channel
				: await Context.User.CreateDMChannelAsync().ConfigureAwait(false);

			await SendRanks(levelModels, channel, position).ConfigureAwait(false);
		}

		private async Task SendRanks(List<LevelModel> levelModels, IMessageChannel channel, int position) {
			const int elementsPerList = 20;

			if(!levelModels.Any()) return;

			var       groupedLevelModels = levelModels.GroupBy(lm => (int) Math.Floor(levelModels.IndexOf(lm) * 1d / elementsPerList));
			var       rankStrings        = new List<string>();
			var       sb                 = new StringBuilder();
			sb.AppendLine(GetText("ranks_header"));

			foreach(var glm in groupedLevelModels) {
				if(!glm.Any()) continue;
				var listNumber = glm.Key + 1;
				sb.Append($"```{GetText("ranks_list_header", listNumber)}");

				foreach(var lm in glm) {
					var user = await Context.Guild.GetUserAsync(lm.UserId).ConfigureAwait(false);
					var namestring = user?.ToString() ?? uow.UsernameHistory.GetLastUsername(lm.UserId) ?? lm.UserId.ToString();
					sb.Append("\n" + GetText("ranks_list_row", $"{position + levelModels.IndexOf(lm),3}", $"{namestring,-37}", $"{lm.Level,3}", $"{lm.XP,6}", $"{LevelModel.GetXpToNextLevel(lm.Level),6}", $"{lm.TotalXP,8}"));
				}

				sb.Append("```");
				rankStrings.Add(sb.ToString());
				sb.Clear();
			}

			foreach(var s in rankStrings) {
				await channel.SendMessageAsync(s).ConfigureAwait(false);
				Thread.Sleep(250);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(1)]
		[OwnerOrGuildPermission(GuildPermission.Administrator)]
		public async Task AddXp(int xp, [Remainder] IUser user = null) {
			user??=Context.User;

			uow.LevelModel.AddXP(Context.Guild.Id, user.Id, xp, Context.Channel.Id);
			await ConfirmLocalized("addxp", xp, user.ToString()).ConfigureAwait(false);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(0)]
		[OwnerOrGuildPermission(GuildPermission.Administrator)]
		public async Task AddXp(int xp, ulong userId) {
			var user = await Context.Guild.GetUserAsync(userId);
			if(user != null) {
				await AddXp(xp, user).ConfigureAwait(false);
				return;
			}

			uow.LevelModel.AddXP(Context.Guild.Id, userId, xp, Context.Channel.Id);
			await ConfirmLocalized("addxp", xp, userId.ToString()).ConfigureAwait(false);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(1)]
		[OwnerOrGuildPermission(GuildPermission.Administrator)]
		public async Task SetXp(int xp, [Remainder] IUser user = null) {
			user??=Context.User;
			uow.LevelModel.SetXP(Context.Guild.Id, user.Id, xp, Context.Channel.Id);
			await ConfirmLocalized("setxp", user.ToString(), xp).ConfigureAwait(false);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(0)]
		[OwnerOrGuildPermission(GuildPermission.Administrator)]
		public async Task SetXp(int xp, ulong userId) {
			var user = await Context.Guild.GetUserAsync(userId);
			if(user != null) {
				await SetXp(xp, user).ConfigureAwait(false);
				return;
			}

			uow.LevelModel.SetXP(Context.Guild.Id, userId, xp, Context.Channel.Id);
			await ConfirmLocalized("setxp", userId, xp).ConfigureAwait(false);
			await uow.SaveChangesAsync(false).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task TurnToXp(long moneyToSpend, [Remainder] IUser user = null) {
			user = user != null && _creds.IsOwner(Context.User) ? user : Context.User;
			
			if(moneyToSpend >= 0) {
				if(moneyToSpend > 0) {
					if(uow.Currency.TryAddCurrencyValue(Context.Guild.Id, user.Id, -moneyToSpend)) {
						var xp = (int) (moneyToSpend * uow.GuildConfigs.For(Context.Guild.Id).TurnToXpMultiplier);
						uow.LevelModel.AddXP(Context.Guild.Id, user.Id, xp, Context.Channel.Id);
						await uow.SaveChangesAsync(false).ConfigureAwait(false);

						await ReplyConfirmLocalized(user == Context.User ? "ttxp_turned_self" : "ttxp_turned_other", moneyToSpend, CurrencySign, xp, user.ToString()).ConfigureAwait(false);
					} else {
						await ReplyErrorLocalized(user == Context.User ? "ttxp_error_no_money_self" : "ttxp_error_no_money_other", user.ToString()).ConfigureAwait(false);
					}
				} else {
					await ReplyErrorLocalized("ttxp_error_zero_value", CurrencySign).ConfigureAwait(false);
				}
			} else {
				await ReplyErrorLocalized("ttxp_error_negative_value").ConfigureAwait(false);
			}
		}
	}
}
