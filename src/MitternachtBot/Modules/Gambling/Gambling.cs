using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Gambling {
	public partial class Gambling : MitternachtTopLevelModule {
		private readonly IBotConfigProvider _bc;
		private readonly DbService _db;
		private readonly CurrencyService _currency;

		private string CurrencyName => _bc.BotConfig.CurrencyName;
		private string CurrencyPluralName => _bc.BotConfig.CurrencyPluralName;
		private string CurrencySign => _bc.BotConfig.CurrencySign;

		public Gambling(IBotConfigProvider bc, DbService db, CurrencyService currency) {
			_bc = bc;
			_db = db;
			_currency = currency;
		}

		public long GetCurrency(ulong id) {
			using(var uow = _db.UnitOfWork) {
				return uow.Currency.GetUserCurrency(id);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Raffle([Remainder] IRole role = null) {
			role = role ?? Context.Guild.EveryoneRole;

			var members = (await role.GetMembersAsync()).Where(u => u.Status != UserStatus.Offline);
			var membersArray = members as IUser[] ?? members.ToArray();
			if(membersArray.Length == 0) {

			}
			var usr = membersArray[new NadekoRandom().Next(0, membersArray.Length)];
			await Context.Channel.SendConfirmAsync($"**{usr.Username}#{usr.Discriminator}**", "🎟 " + GetText("raffled_user"), footer: $"ID: {usr.Id}").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[Priority(1)]
		public async Task Cash([Remainder] IUser user = null) {
			if(user == null)
				await ConfirmLocalized("has", Format.Bold(Context.User.ToString()), $"{GetCurrency(Context.User.Id)} {CurrencySign}").ConfigureAwait(false);
			else
				await ReplyConfirmLocalized("has", Format.Bold(user.ToString()), $"{GetCurrency(user.Id)} {CurrencySign}").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[Priority(0)]
		public async Task Cash(ulong userId) {
			await ReplyConfirmLocalized("has", Format.Code(userId.ToString()), $"{GetCurrency(userId)} {CurrencySign}").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Give(long amount, [Remainder] IGuildUser receiver) {
			if(amount <= 0) {
				await Context.Channel.SendMessageAsync($"Geldbeträge von 0{CurrencySign} oder weniger können nicht verschenkt werden!");
				return;
			}
			if(Context.User.Id == receiver.Id) {
				await Context.Channel.SendMessageAsync("Geld kann man nicht an sich selbst verschenken!");
				return;
			}
			var success = await _currency.RemoveAsync((IGuildUser)Context.User, $"Gift to {receiver.Username} ({receiver.Id}).", amount, false).ConfigureAwait(false);
			if(!success) {
				await ReplyErrorLocalized("not_enough", CurrencyPluralName).ConfigureAwait(false);
				return;
			}
			await _currency.AddAsync(receiver, $"Gift from {Context.User.Username} ({Context.User.Id}).", amount, true).ConfigureAwait(false);
			await ReplyConfirmLocalized("gifted", amount + CurrencySign, Format.Bold(receiver.ToString())).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		[Priority(0)]
		public Task Award(int amount, [Remainder] IGuildUser usr) =>
			Award(amount, usr.Id);

		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		[Priority(1)]
		public async Task Award(int amount, ulong usrId) {
			if(amount <= 0)
				return;

			await _currency.AddAsync(usrId, $"Awarded by bot owner. ({Context.User.Username}/{Context.User.Id})", amount).ConfigureAwait(false);
			await ReplyConfirmLocalized("awarded", amount + CurrencySign, $"<@{usrId}>").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		[Priority(2)]
		public async Task Award(int amount, [Remainder] IRole role) {
			var users = (await Context.Guild.GetUsersAsync()).Where(u => u.GetRoles().Contains(role)).ToList();
			await Task.WhenAll(users.Select(u => _currency.AddAsync(u.Id, $"Awarded by bot owner to **{role.Name}** role. ({Context.User.Username}/{Context.User.Id})", amount))).ConfigureAwait(false);

			await ReplyConfirmLocalized("mass_award", amount + CurrencySign, Format.Bold(users.Count.ToString()), Format.Bold(role.Name)).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task Take(long amount, [Remainder] IGuildUser user) {
			if(amount <= 0)
				return;

			if(await _currency.RemoveAsync(user, $"Taken by bot owner.({Context.User.Username}/{Context.User.Id})", amount, true).ConfigureAwait(false))
				await ReplyConfirmLocalized("take", amount + CurrencySign, Format.Bold(user.ToString())).ConfigureAwait(false);
			else
				await ReplyErrorLocalized("take_fail", amount + CurrencySign, Format.Bold(user.ToString()), CurrencyPluralName).ConfigureAwait(false);
		}


		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		public async Task Take(long amount, [Remainder] ulong usrId) {
			if(amount <= 0)
				return;

			if(await _currency.RemoveAsync(usrId, $"Taken by bot owner.({Context.User.Username}/{Context.User.Id})", amount).ConfigureAwait(false))
				await ReplyConfirmLocalized("take", amount + CurrencySign, $"<@{usrId}>").ConfigureAwait(false);
			else
				await ReplyErrorLocalized("take_fail", amount + CurrencySign, Format.Code(usrId.ToString()), CurrencyPluralName).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task BetRoll(long amount) {
			if(amount < 1)
				return;

			if(!await _currency.RemoveAsync(Context.User, "Betroll Gamble", amount, false).ConfigureAwait(false)) {
				await ReplyErrorLocalized("not_enough", CurrencyPluralName).ConfigureAwait(false);
				return;
			}

			var rnd = new NadekoRandom().Next(0, 101);
			var str = Context.User.Mention + Format.Code(GetText("roll", rnd));
			if(rnd < 67) {
				str += GetText("better_luck");
			} else {
				if(rnd < 91) {
					str += GetText("br_win", (amount * _bc.BotConfig.Betroll67Multiplier) + CurrencySign, 66);
					await _currency.AddAsync(Context.User, "Betroll Gamble",
						(int)(amount * _bc.BotConfig.Betroll67Multiplier), false).ConfigureAwait(false);
				} else if(rnd < 100) {
					str += GetText("br_win", (amount * _bc.BotConfig.Betroll91Multiplier) + CurrencySign, 90);
					await _currency.AddAsync(Context.User, "Betroll Gamble",
						(int)(amount * _bc.BotConfig.Betroll91Multiplier), false).ConfigureAwait(false);
				} else {
					str += GetText("br_win", (amount * _bc.BotConfig.Betroll100Multiplier) + CurrencySign, 100) + " 👑";
					await _currency.AddAsync(Context.User, "Betroll Gamble",
						(int)(amount * _bc.BotConfig.Betroll100Multiplier), false).ConfigureAwait(false);
				}
			}
			await Context.Channel.SendConfirmAsync(str).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task Leaderboard(int page = 1) {
			if(page < 1)
				return;

			const int elpp = 9;
			int currcount;
			using(var uow = _db.UnitOfWork)
				currcount = uow.Currency.GetAll().Count();

			await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, p => {
				var embed = new EmbedBuilder()
					.WithOkColor()
					.WithTitle(CurrencySign + " " + GetText("leaderboard"));
				List<Currency> richest;
				using(var uow = _db.UnitOfWork)
					richest = uow.Currency.GetTopRichest(elpp, elpp * p).ToList();
				if(!richest.Any()) {
					embed.WithDescription(GetText("no_users_found"));
				} else {
					foreach(var c in richest) {
						var user = Context.Guild.GetUserAsync(c.UserId).GetAwaiter().GetResult();
						var username = user?.Username.TrimTo(20, true) ?? c.UserId.ToString();
						embed.AddField($"#{elpp * p + richest.IndexOf(c) + 1} {username}", $"{c.Amount} {CurrencySign}", true);
					}
				}
				return embed;
			}, currcount / elpp, reactUsers: new[] { Context.User as IGuildUser });
		}
	}
}
