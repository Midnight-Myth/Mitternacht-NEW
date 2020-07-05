using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class MuteCommands : MitternachtSubmodule<MuteService> {
			private readonly IUnitOfWork uow;

			public MuteCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			[Priority(0)]
			public async Task SetMuteRole([Remainder] string name) {
				name = name.Trim();
				if(string.IsNullOrWhiteSpace(name))
					return;

				var gc = uow.GuildConfigs.For(Context.Guild.Id);
				gc.MuteRoleName = name;
				await uow.SaveChangesAsync(false).ConfigureAwait(false);
				await ReplyConfirmLocalized("mute_role_set").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			[Priority(1)]
			public Task SetMuteRole([Remainder] IRole role)
				=> SetMuteRole(role.Name);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			[RequireUserPermission(GuildPermission.MuteMembers)]
			[Priority(0)]
			public async Task Mute(IGuildUser user) {
				try {
					await Service.MuteUser(user).ConfigureAwait(false);
					await ReplyConfirmLocalized("user_muted", Format.Bold(user.ToString())).ConfigureAwait(false);
				} catch(Exception e) {
					_log.Warn(e);
					await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			[RequireUserPermission(GuildPermission.MuteMembers)]
			[Priority(1)]
			public async Task Mute(string time, IGuildUser user) {
				const string timeRegex = "((?<days>\\d+)d)?((?<hours>\\d+)h)?((?<minutes>\\d+)m(in)?)?";
				
				var match = Regex.Match(time, timeRegex);

				var days    = string.IsNullOrWhiteSpace(match.Groups["days"   ].Value) ? 0 : Convert.ToInt32(match.Groups["days"   ].Value);
				var hours   = string.IsNullOrWhiteSpace(match.Groups["hours"  ].Value) ? 0 : Convert.ToInt32(match.Groups["hours"  ].Value);
				var minutes = string.IsNullOrWhiteSpace(match.Groups["minutes"].Value) ? 0 : Convert.ToInt32(match.Groups["minutes"].Value);

				var muteTime = days*24*60 + hours*60 + minutes;
				try {
					await Service.TimedMute(user, TimeSpan.FromMinutes(muteTime)).ConfigureAwait(false);
					await ReplyConfirmLocalized("user_muted_time", Format.Bold(user.ToString()), muteTime).ConfigureAwait(false);
				} catch(Exception e) {
					_log.Warn(e);
					await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			[RequireUserPermission(GuildPermission.MuteMembers)]
			[Priority(1)]
			public async Task MuteTime(IGuildUser user) {
				if(user == null)
					return;
				var muteTime = Service.GetMuteTime(user);
				if(muteTime == null || muteTime.Value < DateTime.UtcNow)
					await Context.Channel.SendErrorAsync($"User {(string.IsNullOrWhiteSpace(user.Nickname) ? user.Username : user.Nickname)} ist nicht gemutet.");
				else {
					var ts = muteTime.Value - DateTime.UtcNow;
					var tstring = $"{((int) Math.Floor(ts.TotalDays) == 0 ? "" : (int) Math.Floor(ts.TotalDays) + "d")} {(ts.Hours == 0 ? "" : ts.Hours + "h")} {(ts.Minutes == 0 ? "" : ts.Minutes + "min")} {(ts.Seconds == 0 ? "" : ts.Seconds + "s")}".Trim();
					await Context.Channel.SendConfirmAsync($"User {(string.IsNullOrWhiteSpace(user.Nickname) ? user.Username : user.Nickname)} ist noch {Format.Bold(tstring)} gemutet.");
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(0)]
			public async Task MuteTime()
				=> await MuteTime(Context.User as IGuildUser);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			[RequireUserPermission(GuildPermission.MuteMembers)]
			public async Task Unmute(IGuildUser user) {
				try {
					await Service.UnmuteUser(user).ConfigureAwait(false);
					await ReplyConfirmLocalized("user_unmuted", Format.Bold(user.ToString())).ConfigureAwait(false);
				} catch(Exception e) {
					_log.Warn(e);
					await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
				}
			}
		}
	}
}
