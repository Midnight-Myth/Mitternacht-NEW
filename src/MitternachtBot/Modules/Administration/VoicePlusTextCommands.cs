using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Database;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class VoicePlusTextCommands : MitternachtSubmodule<VoicePlusTextService> {
			private readonly IUnitOfWork uow;

			public VoicePlusTextCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageRoles | GuildPermission.ManageChannels)]
			public async Task VoicePlusText() {
				var guild = Context.Guild;

				var botUser = await guild.GetCurrentUserAsync().ConfigureAwait(false);
				if(!botUser.GuildPermissions.ManageRoles || !botUser.GuildPermissions.ManageChannels) {
					await ReplyErrorLocalized("vt_perms").ConfigureAwait(false);
					return;
				}

				if(!botUser.GuildPermissions.Administrator) {
					try {
						await ReplyErrorLocalized("vt_no_admin").ConfigureAwait(false);
					} catch { }
				}
				try {
					var gc = uow.GuildConfigs.For(guild.Id);
					var isEnabled = gc.VoicePlusTextEnabled = !gc.VoicePlusTextEnabled;
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					if(!isEnabled) {
						foreach(var textChannel in (await guild.GetTextChannelsAsync().ConfigureAwait(false)).Where(c => c.Name.EndsWith("-voice"))) {
							try { await textChannel.DeleteAsync().ConfigureAwait(false); } catch { }
							await Task.Delay(500).ConfigureAwait(false);
						}

						foreach(var role in guild.Roles.Where(c => c.Name.StartsWith("nvoice-"))) {
							try { await role.DeleteAsync().ConfigureAwait(false); } catch { }
							await Task.Delay(500).ConfigureAwait(false);
						}
						await ReplyConfirmLocalized("vt_disabled").ConfigureAwait(false);
						return;
					}

					await ReplyConfirmLocalized("vt_enabled").ConfigureAwait(false);

				} catch(Exception ex) {
					await Context.Channel.SendErrorAsync(ex.ToString()).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageChannels | GuildPermission.ManageRoles)]
			[RequireBotPermission(GuildPermission.ManageChannels | GuildPermission.ManageRoles)]
			public async Task CleanVPlusT() {
				var botUser = await Context.Guild.GetCurrentUserAsync().ConfigureAwait(false);

				var textChannels = await Context.Guild.GetTextChannelsAsync().ConfigureAwait(false);
				var voiceChannels = await Context.Guild.GetVoiceChannelsAsync().ConfigureAwait(false);

				var validTxtChannelNames = voiceChannels.Select(c => Service.GetChannelName(c.Name)).ToArray();
				var invalidTxtChannels = textChannels.Where(c => c.Name.EndsWith("-voice", StringComparison.OrdinalIgnoreCase) && !validTxtChannelNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase));

				foreach(var c in invalidTxtChannels) {
					try { await c.DeleteAsync().ConfigureAwait(false); } catch { }
					await Task.Delay(500).ConfigureAwait(false);
				}

				var validRoleNames = voiceChannels.Select(c => Service.GetRoleName(c)).ToArray();
				var invalidRoles = Context.Guild.Roles.Where(r => r.Name.StartsWith("nvoice-", StringComparison.OrdinalIgnoreCase) && !validRoleNames.Contains(r.Name, StringComparer.OrdinalIgnoreCase));

				foreach(var r in invalidRoles) {
					try { await r.DeleteAsync().ConfigureAwait(false); } catch { }
					await Task.Delay(500).ConfigureAwait(false);
				}

				await ReplyConfirmLocalized("cleaned_up").ConfigureAwait(false);
			}
		}
	}
}