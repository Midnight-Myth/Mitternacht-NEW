using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Games.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Games {
	public partial class Games {
		[Group]
		public class PlantPickCommands : MitternachtSubmodule<PlantAndPickService> {
			private readonly CurrencyService _cs;
			private readonly IBotConfigProvider _bc;
			private readonly IUnitOfWork uow;

			public PlantPickCommands(IBotConfigProvider bc, CurrencyService cs, IUnitOfWork uow) {
				_bc = bc;
				_cs = cs;
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Pick() {
				var channel = (ITextChannel)Context.Channel;

				if(!(await channel.Guild.GetCurrentUserAsync()).GetPermissions(channel).ManageMessages)
					return;


				try { await Context.Message.DeleteAsync().ConfigureAwait(false); } catch { }
				if(!Service.PlantedFlowers.TryRemove(channel.Id, out List<IUserMessage> msgs))
					return;

				await Task.WhenAll(msgs.Where(m => m != null).Select(toDelete => toDelete.DeleteAsync())).ConfigureAwait(false);

				await _cs.AddAsync((IGuildUser)Context.User, $"Picked {_bc.BotConfig.CurrencyPluralName}", msgs.Count, false).ConfigureAwait(false);
				var msg = await ReplyConfirmLocalized("picked", msgs.Count + _bc.BotConfig.CurrencySign)
					.ConfigureAwait(false);
				msg.DeleteAfter(10);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Plant(int amount = 1) {
				if(amount < 1)
					return;

				var removed = await _cs.RemoveAsync((IGuildUser)Context.User, $"Planted a {_bc.BotConfig.CurrencyName}", amount).ConfigureAwait(false);
				if(!removed) {
					await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
					return;
				}

				var (Name, Data) = Service.GetRandomCurrencyImage();

				var msgToSend = GetText("planted", Format.Bold(Context.User.ToString()), $"{amount}{_bc.BotConfig.CurrencySign}", Prefix);

				if(amount > 1)
					msgToSend += $" {GetText("pick_pl", Prefix)}";
				else
					msgToSend += $" {GetText("pick_sn", Prefix)}";

				using var toSend = Data.ToStream();
				var msg = await Context.Channel.SendFileAsync(toSend, Name, msgToSend).ConfigureAwait(false);

				var msgs = new IUserMessage[amount];
				msgs[0] = msg;

				Service.PlantedFlowers.AddOrUpdate(Context.Channel.Id, msgs.ToList(), (id, old) => {
					old.AddRange(msgs);
					return old;
				});
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			public async Task GenCurrency() {
				var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.GenerateCurrencyChannelIds));

				var toAdd = new GCChannelId {
					ChannelId = Context.Channel.Id
				};

				if(!gc.GenerateCurrencyChannelIds.Contains(toAdd)) {
					gc.GenerateCurrencyChannelIds.Add(toAdd);
					await ReplyConfirmLocalized("curgen_enabled").ConfigureAwait(false);
				} else {
					gc.GenerateCurrencyChannelIds.Remove(toAdd);
					await ReplyConfirmLocalized("curgen_disabled").ConfigureAwait(false);
				}

				await uow.SaveChangesAsync(false);
			}
		}
	}
}