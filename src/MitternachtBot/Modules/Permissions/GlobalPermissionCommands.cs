using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.TypeReaders;
using Mitternacht.Extensions;
using Mitternacht.Modules.Permissions.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Permissions {
	public partial class Permissions {
		[Group]
		public class GlobalPermissionCommands : MitternachtSubmodule {
			private GlobalPermissionService _service;
			private readonly IUnitOfWork uow;

			public GlobalPermissionCommands(GlobalPermissionService service, IUnitOfWork uow) {
				_service = service;
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task Lgp() {
				if(!_service.BlockedModules.Any() && !_service.BlockedCommands.Any()) {
					await ReplyErrorLocalized("lgp_none").ConfigureAwait(false);
					return;
				}

				var embed = new EmbedBuilder().WithOkColor();

				if(_service.BlockedModules.Any())
					embed.AddField(efb => efb.WithName(GetText("blocked_modules")).WithValue(string.Join("\n", _service.BlockedModules)).WithIsInline(false));

				if(_service.BlockedCommands.Any())
					embed.AddField(efb => efb.WithName(GetText("blocked_commands")).WithValue(string.Join("\n", _service.BlockedCommands)).WithIsInline(false));

				await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task Gmod(ModuleOrCrInfo module) {
				var moduleName = module.Name.ToLowerInvariant();
				if(_service.BlockedModules.Add(moduleName)) {
					var bc = uow.BotConfig.GetOrCreate();
					bc.BlockedModules.Add(new BlockedCmdOrMdl {
						Name = moduleName,
					});
					uow.SaveChanges(false);

					await ReplyConfirmLocalized("gmod_add", Format.Bold(module.Name)).ConfigureAwait(false);
					return;
				} else if(_service.BlockedModules.TryRemove(moduleName)) {
					var bc = uow.BotConfig.GetOrCreate();
					bc.BlockedModules.RemoveWhere(x => x.Name == moduleName);
					uow.SaveChanges(false);

					await ReplyConfirmLocalized("gmod_remove", Format.Bold(module.Name)).ConfigureAwait(false);
					return;
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task Gcmd(CommandOrCrInfo cmd) {
				var commandName = cmd.Name.ToLowerInvariant();
				if(_service.BlockedCommands.Add(commandName)) {
					var bc = uow.BotConfig.GetOrCreate();
					bc.BlockedCommands.Add(new BlockedCmdOrMdl {
						Name = commandName,
					});
					uow.SaveChanges(false);

					await ReplyConfirmLocalized("gcmd_add", Format.Bold(cmd.Name)).ConfigureAwait(false);
					return;
				} else if(_service.BlockedCommands.TryRemove(commandName)) {
					var bc = uow.BotConfig.GetOrCreate();
					bc.BlockedCommands.RemoveWhere(x => x.Name == commandName);
					uow.SaveChanges(false);

					await ReplyConfirmLocalized("gcmd_remove", Format.Bold(cmd.Name)).ConfigureAwait(false);
					return;
				}
			}
		}
	}
}
