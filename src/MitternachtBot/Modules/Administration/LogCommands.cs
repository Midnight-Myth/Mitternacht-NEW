using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.TypeReaders.Models;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using Mitternacht.Database;
using Mitternacht.Database.Models;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class LogCommands : MitternachtSubmodule<LogCommandService> {
			private readonly IUnitOfWork uow;

			public LogCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			public enum EnableDisable {
				Enable,
				Disable
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			[OwnerOnly]
			public async Task LogServer(PermissionAction action) {
				var channel = (ITextChannel)Context.Channel;
				var logSetting = uow.GuildConfigs.For(channel.Guild.Id, set => set.Include(x => x.LogSetting)).LogSetting;
				logSetting.LogOtherId =
				logSetting.MessageUpdatedId =
				logSetting.MessageDeletedId =
				logSetting.UserJoinedId =
				logSetting.UserLeftId =
				logSetting.UserBannedId =
				logSetting.UserUnbannedId =
				logSetting.UserUpdatedId =
				logSetting.ChannelCreatedId =
				logSetting.ChannelDestroyedId =
				logSetting.ChannelUpdatedId =
				logSetting.LogUserPresenceId =
				logSetting.LogVoicePresenceId =
				logSetting.UserMutedId =
				logSetting.LogVoicePresenceTTSId = (action.Value ? channel.Id : (ulong?)null);

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				if(action.Value)
					await ReplyConfirmLocalized("log_all").ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("log_disabled").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			[OwnerOnly]
			public async Task LogIgnore() {
				var logSetting = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.LogSetting).ThenInclude(x => x.IgnoredChannels)).LogSetting;
				
				var removed = logSetting.IgnoredChannels.RemoveWhere(c => c.ChannelId == Context.Channel.Id);
				if(removed == 0) {
					logSetting.IgnoredChannels.Add(new IgnoredLogChannel {
						ChannelId = Context.Channel.Id
					});
				}

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				if(removed == 0)
					await ReplyConfirmLocalized("log_ignore", Format.Bold($"{MentionUtils.MentionChannel(Context.Channel.Id)}({Context.Channel.Id})")).ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("log_not_ignore", Format.Bold($"{MentionUtils.MentionChannel(Context.Channel.Id)}({Context.Channel.Id})")).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			[OwnerOnly]
			public async Task LogEvents() {
				await Context.Channel.SendConfirmAsync(Format.Bold(GetText("log_events")) + "\n" +
													   $"```fix\n{string.Join(", ", Enum.GetNames(typeof(LogCommandService.LogType)).Cast<string>())}```")
					.ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			[OwnerOnly]
			public async Task Log(LogCommandService.LogType type) {
				ulong? channelId = null;

				var logSetting = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.LogSetting)).LogSetting;
				switch(type) {
					case LogCommandService.LogType.Other:
						channelId = logSetting.LogOtherId = (logSetting.LogOtherId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.MessageUpdated:
						channelId = logSetting.MessageUpdatedId = (logSetting.MessageUpdatedId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.MessageDeleted:
						channelId = logSetting.MessageDeletedId = (logSetting.MessageDeletedId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.UserJoined:
						channelId = logSetting.UserJoinedId = (logSetting.UserJoinedId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.UserLeft:
						channelId = logSetting.UserLeftId = (logSetting.UserLeftId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.UserBanned:
						channelId = logSetting.UserBannedId = (logSetting.UserBannedId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.UserUnbanned:
						channelId = logSetting.UserUnbannedId = (logSetting.UserUnbannedId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.UserUpdated:
						channelId = logSetting.UserUpdatedId = (logSetting.UserUpdatedId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.UserMuted:
						channelId = logSetting.UserMutedId = (logSetting.UserMutedId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.ChannelCreated:
						channelId = logSetting.ChannelCreatedId = (logSetting.ChannelCreatedId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.ChannelDestroyed:
						channelId = logSetting.ChannelDestroyedId = (logSetting.ChannelDestroyedId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.ChannelUpdated:
						channelId = logSetting.ChannelUpdatedId = (logSetting.ChannelUpdatedId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.UserPresence:
						channelId = logSetting.LogUserPresenceId = (logSetting.LogUserPresenceId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.VoicePresence:
						channelId = logSetting.LogVoicePresenceId = (logSetting.LogVoicePresenceId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.VoicePresenceTTS:
						channelId = logSetting.LogVoicePresenceTTSId = (logSetting.LogVoicePresenceTTSId == null ? Context.Channel.Id : default(ulong?));
						break;
					case LogCommandService.LogType.VerificationSteps:
						channelId = logSetting.VerificationSteps = logSetting.VerificationSteps == null ? Context.Channel.Id : default(ulong?);
						break;
					case LogCommandService.LogType.VerificationMessages:
						channelId = logSetting.VerificationMessages = logSetting.VerificationMessages == null ? Context.Channel.Id : default(ulong?);
						break;
				}

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				if(channelId != null)
					await ReplyConfirmLocalized("log", Format.Bold(type.ToString())).ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("log_stop", Format.Bold(type.ToString())).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			[OwnerOnly]
			public async Task LogList() {
				var logChannels = Service.GetLogChannelList(Context.Guild);
				var logChannelString = string.Join("\n", logChannels.Select(kv => $"{kv.Key.ToString()}: {kv.Value?.Mention ?? "--"}").ToList());
				await Context.Channel.SendConfirmAsync(logChannelString, GetText("log_list_title")).ConfigureAwait(false);
			}
		}
	}
}