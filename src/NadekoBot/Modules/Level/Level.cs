using System;
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
			userId = userId != 0 ? userId : Context.User.Id;

			int totalRanks, rank, totalXp, level, currentXp;
			using(var uow = _db.UnitOfWork) {
				var lm = uow.LevelModel.Get(Context.Guild.Id, userId);
				totalXp    = uow.LevelModel.GetTotalXp(Context.Guild.Id, userId);
				level      = uow.LevelModel.GetLevel(Context.Guild.Id, userId);
				currentXp  = uow.LevelModel.GetCurrentXp(Context.Guild.Id, userId);
				totalRanks = uow.LevelModel.GetAll().Count(m => m.TotalXP > 0 && m.GuildId == Context.Guild.Id);
				rank = lm == null
					? -1
					: uow.LevelModel.GetAll().Where(p => p.GuildId == Context.Guild.Id)
						.OrderByDescending(p => p.TotalXP).ToList().IndexOf(lm) + 1;
				await uow.CompleteAsync().ConfigureAwait(false);
			}

			if(userId == Context.User.Id) {
				await Context.Channel.SendMessageAsync(GetText("rank_self",                                   Context.User.Mention, level, currentXp,
																LevelModelRepository.GetXpToNextLevel(level), totalXp,              totalXp > 0 ? rank.ToString() : "-",
																totalRanks)).ConfigureAwait(false);
			} else {
				var user       = await Context.Guild.GetUserAsync(userId).ConfigureAwait(false);
				var namestring = user?.Nickname ?? (user?.Username ?? userId.ToString());
				await Context.Channel.SendMessageAsync(GetText("rank_other",                         Context.User.Mention,                         namestring, level,
																currentXp,                           LevelModelRepository.GetXpToNextLevel(level), totalXp,
																totalXp > 0 ? rank.ToString() : "-", totalRanks)).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Ranks(int count = 20, int position = 1) {
			const int elementsPerList = 20;
			using var uow = _db.UnitOfWork;
			var levelModels = uow.LevelModel
								.GetAll()
								.Where(p => p.TotalXP != 0 && p.GuildId == Context.Guild.Id)
								.OrderByDescending(p => p.TotalXP)
								.Skip(position - 1 <= 0 ? 0 : position - 1)
								.Take(count)
								.ToList();

			if(!levelModels.Any()) return;

			var groupedLevelModels =
				levelModels.GroupBy(lm => (int) Math.Floor(levelModels.IndexOf(lm) * 1d / elementsPerList));
			var rankStrings = new List<string>();
			var sb          = new StringBuilder();
			sb.AppendLine(GetText("ranks_header"));
			foreach(var glm in groupedLevelModels) {
				if(!glm.Any()) continue;
				var listNumber = glm.Key + 1;
				sb.Append($"```{GetText("ranks_list_header", listNumber)}");
				foreach(var lm in glm) {
					var user      = await Context.Guild.GetUserAsync(lm.UserId).ConfigureAwait(false);
					var level     = uow.LevelModel.GetLevel(lm.GuildId, lm.UserId);
					var currentXp = uow.LevelModel.GetCurrentXp(lm.GuildId, lm.UserId);
					sb.Append("\n" + GetText("ranks_list_row",                                   $"{position + levelModels.IndexOf(lm),3}",
											$"{user?.ToString() ?? lm.UserId.ToString(),-37}",   $"{level,3}", $"{currentXp,6}",
											$"{LevelModelRepository.GetXpToNextLevel(level),6}", $"{lm.TotalXP,8}"));
				}

				sb.Append("```");
				rankStrings.Add(sb.ToString());
				sb.Clear();
			}

			var channel = count <= 20
				? Context.Channel
				: await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);
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
