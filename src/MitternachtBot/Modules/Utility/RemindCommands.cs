using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Services;
using Mitternacht.Database;
using Mitternacht.Database.Models;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		[Group]
		public class RemindCommands : MitternachtSubmodule<RemindService> {
			private readonly Regex timeIntervalRegex = new Regex(@"^(?:(?<days>\d+)d)?(?:(?<hours>\d+)h)?(?:(?<minutes>\d+)(?:m|min))?(?:(?<seconds>\d+)s)?$", RegexOptions.Compiled | RegexOptions.Multiline);
			
			private readonly IUnitOfWork uow;
			private readonly GuildTimezoneService _tz;

			public RemindCommands(IUnitOfWork uow, GuildTimezoneService tz) {
				this.uow = uow;
				_tz = tz;
			}

			public enum MeOrHere {
				Me, Here
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[Priority(1)]
			public async Task Remind(MeOrHere meorhere, string timeStr, [Remainder] string message) {
				var target = meorhere == MeOrHere.Me ? Context.User.Id : Context.Channel.Id;

				await RemindInternal(target, meorhere == MeOrHere.Me, timeStr, message).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.ManageMessages)]
			[Priority(0)]
			public async Task Remind(ITextChannel channel, string timeStr, [Remainder] string message) {
				var perms = ((IGuildUser)Context.User).GetPermissions(channel);
				if(!perms.SendMessages || !perms.ViewChannel) {
					await ReplyErrorLocalized("cant_read_or_send").ConfigureAwait(false);
					return;
				}

				await RemindInternal(channel.Id, false, timeStr, message).ConfigureAwait(false);
			}

			public async Task RemindInternal(ulong targetId, bool isPrivate, string timeStr, [Remainder] string message) {
				var m = timeIntervalRegex.Match(timeStr);

				if(m.Success) {
					var timeValues = new Dictionary<string, int>();

					foreach(var groupName in timeIntervalRegex.GetGroupNames()) {
						if(groupName != "0") {
							timeValues[groupName] = !string.IsNullOrEmpty(m.Groups[groupName].Value) && int.TryParse(m.Groups[groupName].Value, out var value) ? value : 0;
						}
					}

					var timespan = new TimeSpan(timeValues["days"], timeValues["hours"], timeValues["minutes"], timeValues["seconds"]);
					var time = DateTime.UtcNow + timespan;

					var rem = new Reminder {
						ChannelId = targetId,
						IsPrivate = isPrivate,
						When      = time,
						Message   = message,
						UserId    = Context.User.Id,
						ServerId  = Context.Guild.Id
					};

					uow.Reminders.Add(rem);
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					var gTime = TimeZoneInfo.ConvertTime(time, _tz.GetTimeZoneOrUtc(Context.Guild.Id));
					_ = Task.Run(() => Service.StartReminder(rem));
					await Context.Channel.SendConfirmAsync($"⏰ {(GetText("remind", !isPrivate ? MentionUtils.MentionChannel(targetId) : Context.User.Username, timespan, gTime, message.SanitizeMentions()))}").ConfigureAwait(false);
				} else {
					await ReplyErrorLocalized("remind_invalid_format").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task RemindTemplate([Remainder] string arg) {
				if(string.IsNullOrWhiteSpace(arg))
					return;

				uow.BotConfig.GetOrCreate().RemindMessageFormat = arg.Trim();
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ReplyConfirmLocalized("remind_template").ConfigureAwait(false);
			}
		}
	}
}
