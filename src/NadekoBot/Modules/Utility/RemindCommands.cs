using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Modules.Utility.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		[Group]
		public class RemindCommands : MitternachtSubmodule<RemindService> {
			private readonly DbService _db;
			private readonly GuildTimezoneService _tz;

			public RemindCommands(DbService db, GuildTimezoneService tz) {
				_db = db;
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

				var _ = RemindInternal(target, meorhere == MeOrHere.Me, timeStr, message).ConfigureAwait(false);
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

				var _ = RemindInternal(channel.Id, false, timeStr, message).ConfigureAwait(false);
			}

			public async Task RemindInternal(ulong targetId, bool isPrivate, string timeStr, [Remainder] string message) {
				var m = Service.Regex.Match(timeStr);

				if(m.Length == 0) {
					await ReplyErrorLocalized("remind_invalid_format").ConfigureAwait(false);
					return;
				}

				var output = "";
				var namesAndValues = new Dictionary<string, int>();

				foreach(var groupName in Service.Regex.GetGroupNames()) {
					if(groupName == "0")
						continue;
					int.TryParse(m.Groups[groupName].Value, out var value);

					if(string.IsNullOrEmpty(m.Groups[groupName].Value)) {
						namesAndValues[groupName] = 0;
						continue;
					}
					if(value < 1 ||
						(groupName == "months" && value > 1) ||
						(groupName == "weeks" && value > 4) ||
						(groupName == "days" && value >= 7) ||
						(groupName == "hours" && value > 23) ||
						(groupName == "minutes" && value > 59)) {
						await Context.Channel.SendErrorAsync($"Invalid {groupName} value.").ConfigureAwait(false);
						return;
					}
					namesAndValues[groupName] = value;
					output += m.Groups[groupName].Value + " " + groupName + " ";
				}
				var time = DateTime.UtcNow + new TimeSpan(30 * namesAndValues["months"] + 7 * namesAndValues["weeks"] + namesAndValues["days"], namesAndValues["hours"], namesAndValues["minutes"],0);

				var rem = new Reminder {
					ChannelId = targetId,
					IsPrivate = isPrivate,
					When      = time,
					Message   = message,
					UserId    = Context.User.Id,
					ServerId  = Context.Guild.Id
				};

				using var uow = _db.UnitOfWork;
				uow.Reminders.Add(rem);
				await uow.CompleteAsync().ConfigureAwait(false);

				var gTime = TimeZoneInfo.ConvertTime(time, _tz.GetTimeZoneOrUtc(Context.Guild.Id));
				try {
					await Context.Channel.SendConfirmAsync($"⏰ {GetText("remind", Format.Bold(!isPrivate ? $"<#{targetId}>" : Context.User.Username), Format.Bold(message.SanitizeMentions()), Format.Bold(output), gTime, gTime)}").ConfigureAwait(false);
				} catch {
					// ignored
				}
				await Service.StartReminder(rem).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task RemindTemplate([Remainder] string arg) {
				if(string.IsNullOrWhiteSpace(arg))
					return;

				using var uow = _db.UnitOfWork;
				uow.BotConfig.GetOrCreate().RemindMessageFormat = arg.Trim();
				await uow.CompleteAsync().ConfigureAwait(false);

				await ReplyConfirmLocalized("remind_template").ConfigureAwait(false);
			}
		}
	}
}
