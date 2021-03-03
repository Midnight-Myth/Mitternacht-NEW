using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Database;

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
				await ConfirmLocalized("mute_role_set").ConfigureAwait(false);
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
					await ConfirmLocalized("user_muted", Format.Bold(user.ToString())).ConfigureAwait(false);
				} catch(Exception e) {
					_log.Warn(e);
					await ErrorLocalized("mute_error").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			[RequireUserPermission(GuildPermission.MuteMembers)]
			[Priority(1)]
			public async Task Mute(IGuildUser user, string time) {
				const string timeRegex = "((?<days>\\d+)d)?((?<hours>\\d+)h)?((?<minutes>\\d+)m(in)?)?((?<seconds>\\d+)s)?";

				var match = Regex.Match(time, timeRegex);

				if(match.Success) {
					var days     = string.IsNullOrWhiteSpace(match.Groups["days"   ].Value) ? 0 : Convert.ToInt32(match.Groups["days"   ].Value);
					var hours    = string.IsNullOrWhiteSpace(match.Groups["hours"  ].Value) ? 0 : Convert.ToInt32(match.Groups["hours"  ].Value);
					var minutes  = string.IsNullOrWhiteSpace(match.Groups["minutes"].Value) ? 0 : Convert.ToInt32(match.Groups["minutes"].Value);
					var seconds  = string.IsNullOrWhiteSpace(match.Groups["seconds"].Value) ? 0 : Convert.ToInt32(match.Groups["seconds"].Value);

					var muteTime = days*24*60*60 + hours*60*60 + minutes*60 + seconds;

					if(muteTime == 0) {
						await Mute(user).ConfigureAwait(false);
					} else {
						try {
							await Service.TimedMute(user, TimeSpan.FromSeconds(muteTime)).ConfigureAwait(false);
							await ConfirmLocalized("user_muted_time", Format.Bold(user.ToString()), muteTime).ConfigureAwait(false);
						} catch(Exception e) {
							_log.Warn(e);
							await ErrorLocalized("mute_error").ConfigureAwait(false);
						}
					}
				} else {
					await ErrorLocalized("mute_error_timeformat", time).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			[RequireUserPermission(GuildPermission.MuteMembers)]
			[Priority(1)]
			public async Task MuteTime(IGuildUser guildUser) {
				guildUser ??= Context.User as IGuildUser;
				var gc       = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.MutedUsers).Include(x => x.UnmuteTimers));
				var muted    = gc.MutedUsers.Any(mu => mu.UserId == guildUser.Id);
				var muteTime = gc.UnmuteTimers.FirstOrDefault(ut => ut.UserId == guildUser.Id)?.UnmuteAt;

				if(muted && muteTime != null && muteTime.Value >= DateTime.UtcNow) {
					var timeSpan       = muteTime.Value - DateTime.UtcNow;
					var timeSpanString = $"{((int) Math.Floor(timeSpan.TotalDays) == 0 ? "" : (int) Math.Floor(timeSpan.TotalDays) + "d")} {(timeSpan.Hours == 0 ? "" : timeSpan.Hours + "h")} {(timeSpan.Minutes == 0 ? "" : timeSpan.Minutes + "min")} {(timeSpan.Seconds == 0 ? "" : timeSpan.Seconds + "s")}".Trim();

					await ConfirmLocalized("mutetime_muted", guildUser.ToString(), timeSpanString).ConfigureAwait(false);
				} else if(muted) {
					await ConfirmLocalized("mutetime_permanent", guildUser.ToString()).ConfigureAwait(false);
				} else {
					await ErrorLocalized("mutetime_not_muted", guildUser.ToString()).ConfigureAwait(false);
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
					await ConfirmLocalized("user_unmuted", Format.Bold(user.ToString())).ConfigureAwait(false);
				} catch(Exception e) {
					_log.Warn(e);
					await ErrorLocalized("mute_error").ConfigureAwait(false);
				}
			}
		}
	}
}
