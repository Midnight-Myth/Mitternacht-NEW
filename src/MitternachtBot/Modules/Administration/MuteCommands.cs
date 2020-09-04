using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
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
			public async Task Mute(IGuildUser user, string time) {
				const string timeRegex = "((?<days>\\d+)d)?((?<hours>\\d+)h)?((?<minutes>\\d+)m(in)?)?";

				var match = Regex.Match(time, timeRegex);

				if(match.Success) {
					var days    = string.IsNullOrWhiteSpace(match.Groups["days"   ].Value) ? 0 : Convert.ToInt32(match.Groups["days"   ].Value);
					var hours   = string.IsNullOrWhiteSpace(match.Groups["hours"  ].Value) ? 0 : Convert.ToInt32(match.Groups["hours"  ].Value);
					var minutes = string.IsNullOrWhiteSpace(match.Groups["minutes"].Value) ? 0 : Convert.ToInt32(match.Groups["minutes"].Value);

					var muteTime = days*24*60 + hours*60 + minutes;

					if(muteTime == 0) {
						await Mute(user).ConfigureAwait(false);
					} else {
						try {
							await Service.TimedMute(user, TimeSpan.FromMinutes(muteTime)).ConfigureAwait(false);
							await ReplyConfirmLocalized("user_muted_time", Format.Bold(user.ToString()), muteTime).ConfigureAwait(false);
						} catch(Exception e) {
							_log.Warn(e);
							await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
						}
					}
				} else {
					await ReplyErrorLocalized("mute_error_timeformat", time).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.KickMembers)]
			[RequireUserPermission(GuildPermission.MuteMembers)]
			[Priority(1)]
			public async Task MuteTime(IGuildUser guildUser) {
				guildUser ??= Context.User as IGuildUser;
				var muteTime = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.UnmuteTimers)).UnmuteTimers.FirstOrDefault(ut => ut.UserId == guildUser.Id)?.UnmuteAt;

				if(muteTime != null && muteTime.Value >= DateTime.UtcNow) {
					var timeSpan       = muteTime.Value - DateTime.UtcNow;
					var timeSpanString = $"{((int) Math.Floor(timeSpan.TotalDays) == 0 ? "" : (int) Math.Floor(timeSpan.TotalDays) + "d")} {(timeSpan.Hours == 0 ? "" : timeSpan.Hours + "h")} {(timeSpan.Minutes == 0 ? "" : timeSpan.Minutes + "min")} {(timeSpan.Seconds == 0 ? "" : timeSpan.Seconds + "s")}".Trim();

					await Context.Channel.SendConfirmAsync($"User {guildUser} ist noch {Format.Bold(timeSpanString)} gemutet.");
				} else {
					await Context.Channel.SendErrorAsync($"User {guildUser} ist nicht gemutet.");
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
