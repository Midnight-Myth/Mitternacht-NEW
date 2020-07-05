using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.Collections;
using Mitternacht.Extensions;
using Mitternacht.Modules.Permissions.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Permissions {
	public partial class Permissions {
		[Group]
		public class CommandCooldownCommands : MitternachtSubmodule<CommandCooldownService> {
			private readonly IUnitOfWork uow;

			public CommandCooldownCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task CommandCooldown(CommandInfo command, int secs) {
				if(secs >= 0) {
					var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.CommandCooldowns));
					gc.CommandCooldowns.RemoveWhere(cc => cc.CommandName.Equals(command.Aliases.First(), StringComparison.OrdinalIgnoreCase));

					if(secs != 0) {
						var cc = new CommandCooldown() {
							CommandName = command.Aliases.First(),
							Seconds = secs,
						};
						gc.CommandCooldowns.Add(cc);
					}
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					if(secs == 0) {
						var activeCds = Service.ActiveCooldowns.GetOrAdd(Context.Guild.Id, new ConcurrentHashSet<ActiveCooldown>());
						activeCds.RemoveWhere(ac => ac.Command.Equals(command.Aliases.First(), StringComparison.OrdinalIgnoreCase));

						await ReplyConfirmLocalized("cmdcd_cleared", Format.Bold(command.Aliases.First())).ConfigureAwait(false);
					} else {
						await ReplyConfirmLocalized("cmdcd_add", Format.Bold(command.Aliases.First()), Format.Bold(secs.ToString())).ConfigureAwait(false);
					}
				} else {
					await ReplyErrorLocalized("commandcooldown_invalid_time").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task CommandCooldowns() {
				var commandCooldowns = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.CommandCooldowns)).CommandCooldowns;

				if(commandCooldowns.Any()) {
					await Context.Channel.SendTableAsync("", commandCooldowns.Select(c => $"{c.CommandName}: {c.Seconds}{GetText("sec")}"), s => $"{s,-30}", 2).ConfigureAwait(false);
				} else {
					await ReplyConfirmLocalized("cmdcd_none").ConfigureAwait(false);
				}
			}
		}
	}
}
