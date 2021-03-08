using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Database;
using Mitternacht.Services.Impl;
using MoreLinq;

namespace Mitternacht.Modules.Gambling {
	public partial class Gambling : MitternachtTopLevelModule {
		private readonly IBotConfigProvider _bc;
		private readonly IUnitOfWork uow;
		private readonly CurrencyService _currency;
		private readonly DbService _db;

		private string CurrencyPluralName => _bc.BotConfig.CurrencyPluralName;
		private string CurrencySign => _bc.BotConfig.CurrencySign;

		public Gambling(IBotConfigProvider bc, IUnitOfWork uow, CurrencyService currency, DbService db) {
			_bc = bc;
			this.uow = uow;
			_currency = currency;
			_db = db;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Raffle([Remainder] IRole role = null) {
			role ??= Context.Guild.EveryoneRole;

			var members = (await role.GetMembersAsync()).Where(u => u.Status != UserStatus.Offline);
			var membersArray = members as IUser[] ?? members.ToArray();
			//TODO: This breaks when membersArray has no elements.
			var user = membersArray.RandomSubset(1).First();
			await Context.Channel.SendConfirmAsync($"**{user.Username}#{user.Discriminator}**", $"🎟 {GetText("raffled_user")}", footer: $"ID: {user.Id}").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(1)]
		public async Task Cash([Remainder] IUser user = null) {
			if(user == null)
				await ConfirmLocalized("has", Format.Bold(Context.User.ToString()), $"{uow.Currency.GetUserCurrencyValue(Context.Guild.Id, Context.User.Id)} {CurrencySign}").ConfigureAwait(false);
			else
				await ReplyConfirmLocalized("has", Format.Bold(user.ToString()), $"{uow.Currency.GetUserCurrencyValue(Context.Guild.Id, user.Id)} {CurrencySign}").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[Priority(0)]
		public async Task Cash(ulong userId) {
			await ReplyConfirmLocalized("has", Format.Code(userId.ToString()), $"{uow.Currency.GetUserCurrencyValue(Context.Guild.Id, userId)} {CurrencySign}").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Give(long amount, [Remainder] IGuildUser receiver) {
			if(amount > 0) {
				if(Context.User.Id != receiver.Id) {
					var reason = $"Gift to {receiver.Username} ({receiver.Id}).";

					if(await _currency.RemoveAsync((IGuildUser)Context.User, reason, amount).ConfigureAwait(false)) {
						await _currency.AddAsync(receiver, $"Gift from {Context.User.Username} ({Context.User.Id}).", amount).ConfigureAwait(false);
						await ReplyConfirmLocalized("gifted", $"{amount}{CurrencySign}", Format.Bold(receiver.ToString())).ConfigureAwait(false);

						try {
							await receiver.SendConfirmAsync($"`You received:` {amount}{CurrencySign} on Server with ID {receiver.GuildId}.\n`Reason:` {reason}").ConfigureAwait(false);
						} catch { }
					} else {
						await ReplyErrorLocalized("not_enough", CurrencyPluralName).ConfigureAwait(false);
					}
				} else {
					// FIXME: Use translations.
					await Context.Channel.SendMessageAsync("Geld kann man nicht an sich selbst verschenken!");
				}
			} else {
				// FIXME: Use translations.
				await Context.Channel.SendMessageAsync($"Geldbeträge von 0{CurrencySign} oder weniger können nicht verschenkt werden!");
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOrGuildPermission(GuildPermission.Administrator)]
		[Priority(0)]
		public Task Award(int amount, [Remainder] IGuildUser user)
			=> Award(amount, user.Id);

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOrGuildPermission(GuildPermission.Administrator)]
		[Priority(1)]
		public async Task Award(int amount, ulong userId) {
			if(amount > 0) {
				await _currency.AddAsync(Context.Guild.Id, userId, $"Awarded by bot owner. ({Context.User.Username}/{Context.User.Id})", amount).ConfigureAwait(false);
				await ReplyConfirmLocalized("awarded", $"{amount}{CurrencySign}", MentionUtils.MentionUser(userId)).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOrGuildPermission(GuildPermission.Administrator)]
		[Priority(2)]
		public async Task Award(int amount, [Remainder] IRole role) {
			var users = (await Context.Guild.GetUsersAsync()).Where(u => u.GetRoles().Contains(role)).ToList();
			await Task.WhenAll(users.Select(u => _currency.AddAsync(Context.Guild.Id, u.Id, $"Awarded by bot owner to **{role.Name}** role. ({Context.User.Username}/{Context.User.Id})", amount))).ConfigureAwait(false);

			await ReplyConfirmLocalized("mass_award", $"{amount}{CurrencySign}", Format.Bold(users.Count.ToString()), Format.Bold(role.Name)).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOrGuildPermission(GuildPermission.Administrator)]
		public async Task Take(long amount, [Remainder] IGuildUser user) {
			if(amount > 0) {
				var reason = $"Taken by bot owner.({Context.User.Username}/{Context.User.Id})";
				if(await _currency.RemoveAsync(user, reason, amount).ConfigureAwait(false)) {
					await ReplyConfirmLocalized("take", $"{amount}{CurrencySign}", Format.Bold(user.ToString())).ConfigureAwait(false);
					
					try {
						// FIXME: Use translations.
						await user.SendErrorAsync($"`You lost:` {amount}{CurrencySign} on Server with ID {user.GuildId}.\n`Reason:` {reason}").ConfigureAwait(false);
					} catch { }
				} else {
					await ReplyErrorLocalized("take_fail", $"{amount}{CurrencySign}", Format.Bold(user.ToString()), CurrencyPluralName).ConfigureAwait(false);
				}
			}
		}


		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOrGuildPermission(GuildPermission.Administrator)]
		public async Task Take(long amount, [Remainder] ulong userId) {
			if(amount > 0) {
				if(await _currency.RemoveAsync(Context.Guild.Id, userId, $"Taken by bot owner.({Context.User.Username}/{Context.User.Id})", amount).ConfigureAwait(false))
					await ReplyConfirmLocalized("take", $"{amount}{CurrencySign}", $"<@{userId}>").ConfigureAwait(false);
				else
					await ReplyErrorLocalized("take_fail", $"{amount}{CurrencySign}", Format.Code(userId.ToString()), CurrencyPluralName).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task BetRoll(long amount) {
			if(amount >= 1) {
				var guildUser = (IGuildUser) Context.User;

				if(await _currency.RemoveAsync(guildUser, "Betroll Gamble", amount).ConfigureAwait(false)) {
					var rnd = new NadekoRandom().Next(0, 101);
					var str = $"{Context.User.Mention}{Format.Code(GetText("roll", rnd))}";
					if(rnd < 67) {
						str += GetText("better_luck");
					} else {
						if(rnd < 91) {
							str += GetText("br_win", $"{amount * _bc.BotConfig.Betroll67Multiplier}{CurrencySign}", 66);
							await _currency.AddAsync(guildUser, "Betroll Gamble", (int)(amount * _bc.BotConfig.Betroll67Multiplier)).ConfigureAwait(false);
						} else if(rnd < 100) {
							str += GetText("br_win", $"{amount * _bc.BotConfig.Betroll91Multiplier}{CurrencySign}", 90);
							await _currency.AddAsync(guildUser, "Betroll Gamble", (int)(amount * _bc.BotConfig.Betroll91Multiplier)).ConfigureAwait(false);
						} else {
							str += $"{GetText("br_win", $"{amount * _bc.BotConfig.Betroll100Multiplier}{CurrencySign}", 100)} 👑";
							await _currency.AddAsync(guildUser, "Betroll Gamble", (int)(amount * _bc.BotConfig.Betroll100Multiplier)).ConfigureAwait(false);
						}
					}
					await Context.Channel.SendConfirmAsync(str).ConfigureAwait(false);
				} else {
					await ReplyErrorLocalized("not_enough", CurrencyPluralName).ConfigureAwait(false);
				}
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task Leaderboard(int page = 1) {
			if(page >= 1) {
				const int elementsPerPage = 9;
				var currencyCount = uow.Currency.GetAll().Count();

				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, async currentPage => {
					var embedBuilder = new EmbedBuilder().WithOkColor().WithTitle($"{CurrencySign} {GetText("leaderboard")}");
					using var unitOfWork = _db.UnitOfWork;
					var leaderboardPage = unitOfWork.Currency.OrderByAmount(Context.Guild.Id).Skip(elementsPerPage * currentPage).Take(elementsPerPage).ToList();

					if(leaderboardPage.Any()) {
						foreach(var c in leaderboardPage) {
							var user     = await Context.Guild.GetUserAsync(c.UserId).ConfigureAwait(false);
							var username = user?.Username.TrimTo(20, true) ?? c.UserId.ToString();

							embedBuilder.AddField($"#{elementsPerPage * currentPage + leaderboardPage.IndexOf(c) + 1} {username}", $"{c.Amount} {CurrencySign}", true);
						}
					} else {
						embedBuilder.WithDescription(GetText("no_users_found"));
					}

					return embedBuilder;
				}, (int)Math.Floor(currencyCount * 1d / elementsPerPage), reactUsers: new[] { Context.User as IGuildUser });
			}
		}
	}
}
