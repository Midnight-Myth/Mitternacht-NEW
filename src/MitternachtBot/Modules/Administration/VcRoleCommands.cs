using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Database;
using Mitternacht.Database.Models;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class VcRoleCommands : MitternachtSubmodule<VcRoleService> {
			private readonly IUnitOfWork uow;

			public VcRoleCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			[RequireUserPermission(GuildPermission.ManageChannels)]
			[RequireBotPermission(GuildPermission.ManageRoles)]
			//todo 999 discord.net [RequireBotPermission(GuildPermission.ManageChannels)]
			[RequireContext(ContextType.Guild)]
			public async Task VcRole([Remainder] IRole role = null) {
				var user = (IGuildUser) Context.User;

				var vc = user.VoiceChannel;

				if(vc == null || vc.GuildId != user.GuildId) {
					await ReplyErrorLocalized("must_be_in_voice").ConfigureAwait(false);
					return;
				}

				var voiceChannelRoleInfos = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.VcRoleInfos)).VcRoleInfos;
				voiceChannelRoleInfos.RemoveWhere(x => x.VoiceChannelId == vc.Id);

				if(role == null) {
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized("vcrole_removed", Format.Bold(vc.Name)).ConfigureAwait(false);
				} else {
					voiceChannelRoleInfos.Add(new VcRoleInfo {
						VoiceChannelId = vc.Id,
						RoleId = role.Id,
					});
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized("vcrole_added", Format.Bold(vc.Name), Format.Bold(role.Name)).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task VcRoleList() {
				var voiceChannelRoleInfos = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.VcRoleInfos)).VcRoleInfos;

				var text = !voiceChannelRoleInfos.Any()
					? GetText("no_vcroles")
					: string.Join("\n", await voiceChannelRoleInfos.ToAsyncEnumerable().SelectAwait(async x => $"{Format.Bold((await Context.Guild.GetVoiceChannelAsync(x.VoiceChannelId).ConfigureAwait(false))?.Name ?? x.VoiceChannelId.ToString())} => {(Context.Guild.GetRole(x.RoleId).Name ?? x.RoleId.ToString())}").ToListAsync());

				await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor().WithTitle(GetText("vc_role_list")).WithDescription(text)).ConfigureAwait(false);
			}
		}
	}
}