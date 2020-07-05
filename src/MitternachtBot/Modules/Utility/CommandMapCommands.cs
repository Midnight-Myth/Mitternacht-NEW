using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		[Group]
		public class CommandMapCommands : MitternachtSubmodule<CommandMapService> {
			private readonly IUnitOfWork uow;
			private readonly DiscordSocketClient _client;

			public CommandMapCommands(IUnitOfWork uow, DiscordSocketClient client) {
				this.uow = uow;
				_client = client;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireUserPermission(GuildPermission.Administrator)]
			[RequireContext(ContextType.Guild)]
			public async Task Alias(string trigger, [Remainder] string mapping = null) {
				if(string.IsNullOrWhiteSpace(trigger))
					return;

				trigger = trigger.Trim();

				var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.CommandAliases));

				if(string.IsNullOrWhiteSpace(mapping)) {
					gc.CommandAliases.RemoveWhere(x => x.Trigger == trigger);
					uow.SaveChanges(false);

					await ReplyConfirmLocalized("alias_removed", Format.Code(trigger)).ConfigureAwait(false);
				} else {
					gc.CommandAliases.RemoveWhere(x => x.Trigger == trigger);
					gc.CommandAliases.Add(new CommandAlias {
						Mapping = mapping,
						Trigger = trigger,
					});
					uow.SaveChanges(false);

					await ReplyConfirmLocalized("alias_added", Format.Code(trigger), Format.Code(mapping)).ConfigureAwait(false);
				}
			}


			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task AliasList(int page = 1) {
				page -= 1;

				if(page < 0)
					return;

				var commandAliases = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.CommandAliases)).CommandAliases.ToArray();

				if(commandAliases.Any()) {
					await Context.Channel.SendPaginatedConfirmAsync(_client, page, curPage => {
						return new EmbedBuilder().WithOkColor()
							.WithTitle(GetText("alias_list"))
							.WithDescription(string.Join("\n", commandAliases.Skip(curPage * 10).Take(10).Select(x => $"`{x.Trigger}` => `{x.Mapping}`")));
					}, commandAliases.Length / 10, reactUsers: new[] { Context.User as IGuildUser }).ConfigureAwait(false);
				} else {
					await ReplyErrorLocalized("aliases_none").ConfigureAwait(false);
				}
			}
		}
	}
}