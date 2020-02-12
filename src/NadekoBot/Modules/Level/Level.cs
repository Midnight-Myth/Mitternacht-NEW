﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Level.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Database.Repositories.Impl;

namespace Mitternacht.Modules.Level {
	[Group]
	public partial class Level : MitternachtTopLevelModule<LevelService> {
		private readonly IBotConfigProvider _bc;
		private readonly DbService          _db;
		private readonly IBotCredentials    _creds;

		private string CurrencySign => _bc.BotConfig.CurrencySign;

		public Level(IBotConfigProvider bc, IBotCredentials creds, DbService db) {
			_bc    = bc;
			_db    = db;
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

			using var uow = _db.UnitOfWork;
			var lm        = uow.LevelModel.Get(Context.Guild.Id, userId);

			if(lm != null) {
				var guildUserIds = await Context.Guild.GetUserIdsAsync();
				var levelModels  = uow.LevelModel.GetAllSortedForRanks(Context.Guild.Id, guildUserIds);

				var rank         = levelModels.ToList().IndexOf(lm) + 1;
				var totalRanks   = levelModels.Count();
				var rankString   = lm.TotalXP > 0 && rank > 0 ? rank.ToString() : "-";

				if(userId == Context.User.Id) {
					await Context.Channel.SendMessageAsync(GetText("rank_self", Context.User.Mention, lm.Level, lm.CurrentXP, LevelModel.GetXpToNextLevel(lm.Level), lm.TotalXP, rankString, totalRanks)).ConfigureAwait(false);
				} else {
					var user       = await Context.Guild.GetUserAsync(userId).ConfigureAwait(false);
					var namestring = user?.Nickname ?? (user?.Username ?? userId.ToString());
					await Context.Channel.SendMessageAsync(GetText("rank_other", Context.User.Mention, namestring, lm.Level, lm.CurrentXP, LevelModel.GetXpToNextLevel(lm.Level), lm.TotalXP, rankString, totalRanks)).ConfigureAwait(false);
				}
			} else {
				await ReplyErrorLocalized("rank_not_found").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Ranks(int count = 20, int position = 1) {
			using var uow = _db.UnitOfWork;
			
			var guildUserIds = await Context.Guild.GetUserIdsAsync();
			
			var levelModels = uow.LevelModel
				.GetAllSortedForRanks(Context.Guild.Id, guildUserIds)
				.Skip(position <= 1 ? 0 : position - 1)
				.Take(count)
				.ToList();

			var channel = count <= 20
				? Context.Channel
				: await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);

			await SendRanks(levelModels, channel, position).ConfigureAwait(false);
		}

		private async Task SendRanks(List<LevelModel> levelModels, IMessageChannel channel, int position) {
			const int elementsPerList = 20;

			if(!levelModels.Any()) return;

			using var uow                = _db.UnitOfWork;
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
					sb.Append("\n" + GetText("ranks_list_row", $"{position + levelModels.IndexOf(lm),3}", $"{user?.ToString() ?? lm.UserId.ToString(),-37}", $"{lm.Level,3}", $"{lm.CurrentXP,6}", $"{LevelModel.GetXpToNextLevel(lm.Level),6}", $"{lm.TotalXP,8}"));
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
		[OwnerOnly]
		public async Task AddXp(int xp, [Remainder] IUser user = null) {
			user??=Context.User;

			using var uow = _db.UnitOfWork;
			uow.LevelModel.AddXp(Context.Guild.Id, user.Id, xp, Context.Channel.Id);
			await ConfirmLocalized("addxp", xp, user.ToString()).ConfigureAwait(false);
			await uow.CompleteAsync().ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(0)]
		[OwnerOnly]
		public async Task AddXp(int xp, ulong userId) {
			var user = await Context.Guild.GetUserAsync(userId);
			if(user != null) {
				await AddXp(xp, user).ConfigureAwait(false);
				return;
			}

			using var uow = _db.UnitOfWork;
			uow.LevelModel.AddXp(Context.Guild.Id, userId, xp, Context.Channel.Id);
			await ConfirmLocalized("addxp", xp, userId.ToString()).ConfigureAwait(false);
			await uow.CompleteAsync().ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(1)]
		[OwnerOnly]
		public async Task SetXp(int xp, [Remainder] IUser user = null) {
			user??=Context.User;
			using var uow = _db.UnitOfWork;
			uow.LevelModel.SetXp(Context.Guild.Id, user.Id, xp, Context.Channel.Id);
			await ConfirmLocalized("setxp", user.ToString(), xp).ConfigureAwait(false);
			await uow.CompleteAsync().ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(0)]
		[OwnerOnly]
		public async Task SetXp(int xp, ulong userId) {
			var user = await Context.Guild.GetUserAsync(userId);
			if(user != null) {
				await SetXp(xp, user).ConfigureAwait(false);
				return;
			}

			using var uow = _db.UnitOfWork;
			uow.LevelModel.SetXp(Context.Guild.Id, userId, xp, Context.Channel.Id);
			await ConfirmLocalized("setxp", userId, xp).ConfigureAwait(false);
			await uow.CompleteAsync().ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task TurnToXp(long moneyToSpend, [Remainder] IUser user = null) {
			user = user != null && _creds.IsOwner(Context.User) ? user : Context.User;
			if(moneyToSpend < 0) {
				await ReplyErrorLocalized("ttxp_error_negative_value").ConfigureAwait(false);
				return;
			}

			if(moneyToSpend == 0) {
				await ReplyErrorLocalized("ttxp_error_zero_value", CurrencySign).ConfigureAwait(false);
				return;
			}

			using var uow = _db.UnitOfWork;
			if(!uow.Currency.TryUpdateState(user.Id, -moneyToSpend)) {
				if(user == Context.User)
					await ReplyErrorLocalized("ttxp_error_no_money_self").ConfigureAwait(false);
				else await ReplyErrorLocalized("ttxp_error_no_money_other", user.ToString()).ConfigureAwait(false);
				return;
			}

			var xp = (int) (moneyToSpend * uow.GuildConfigs.For(Context.Guild.Id, set => set).TurnToXpMultiplier);
			uow.LevelModel.AddXp(Context.Guild.Id, user.Id, xp, Context.Channel.Id);
			if(user == Context.User)
				await ReplyConfirmLocalized("ttxp_turned_self", moneyToSpend, CurrencySign, xp)
					.ConfigureAwait(false);
			else
				await ReplyConfirmLocalized("ttxp_turned_other", user.ToString(), moneyToSpend, CurrencySign, xp)
					.ConfigureAwait(false);
			await uow.CompleteAsync().ConfigureAwait(false);
		}
	}
}
