using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.TypeReaders.Models;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class LogCommands : MitternachtSubmodule<LogCommandService>
        {
            private readonly DbService _db;

            public LogCommands(DbService db)
            {
                _db = db;
            }

            public enum EnableDisable
            {
                Enable,
                Disable
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogServer(PermissionAction action)
            {
                var channel = (ITextChannel)Context.Channel;
                LogSetting logSetting;
                using (var uow = _db.UnitOfWork)
                {
                    logSetting = uow.GuildConfigs.LogSettingsFor(channel.Guild.Id).LogSetting;
                    Service.GuildLogSettings.AddOrUpdate(channel.Guild.Id, (id) => logSetting, (id, old) => logSetting);
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

                    await uow.SaveChangesAsync().ConfigureAwait(false);
                }
                if (action.Value)
                    await ReplyConfirmLocalized("log_all").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("log_disabled").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogIgnore()
            {
                var channel = (ITextChannel)Context.Channel;
                int removed;
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.LogSettingsFor(channel.Guild.Id);
                    LogSetting logSetting = Service.GuildLogSettings.GetOrAdd(channel.Guild.Id, (id) => config.LogSetting);
                    removed = logSetting.IgnoredChannels.RemoveWhere(ilc => ilc.ChannelId == channel.Id);
                    config.LogSetting.IgnoredChannels.RemoveWhere(ilc => ilc.ChannelId == channel.Id);
                    if (removed == 0)
                    {
                        var toAdd = new IgnoredLogChannel { ChannelId = channel.Id };
                        logSetting.IgnoredChannels.Add(toAdd);
                        config.LogSetting.IgnoredChannels.Add(toAdd);
                    }
                    await uow.SaveChangesAsync().ConfigureAwait(false);
                }

                if (removed == 0)
                    await ReplyConfirmLocalized("log_ignore", Format.Bold(channel.Mention + "(" + channel.Id + ")")).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("log_not_ignore", Format.Bold(channel.Mention + "(" + channel.Id + ")")).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task LogEvents()
            {
                await Context.Channel.SendConfirmAsync(Format.Bold(GetText("log_events")) + "\n" +
                                                       $"```fix\n{string.Join(", ", Enum.GetNames(typeof(LogCommandService.LogType)).Cast<string>())}```")
                    .ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task Log(LogCommandService.LogType type)
            {
                var channel = (ITextChannel)Context.Channel;
                ulong? channelId = null;
                using (var uow = _db.UnitOfWork)
                {
                    var logSetting = uow.GuildConfigs.LogSettingsFor(channel.Guild.Id).LogSetting;
                    Service.GuildLogSettings.AddOrUpdate(channel.Guild.Id, (id) => logSetting, (id, old) => logSetting);
                    switch (type)
                    {
                        case LogCommandService.LogType.Other:
                            channelId = logSetting.LogOtherId = (logSetting.LogOtherId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.MessageUpdated:
                            channelId = logSetting.MessageUpdatedId = (logSetting.MessageUpdatedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.MessageDeleted:
                            channelId = logSetting.MessageDeletedId = (logSetting.MessageDeletedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.UserJoined:
                            channelId = logSetting.UserJoinedId = (logSetting.UserJoinedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.UserLeft:
                            channelId = logSetting.UserLeftId = (logSetting.UserLeftId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.UserBanned:
                            channelId = logSetting.UserBannedId = (logSetting.UserBannedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.UserUnbanned:
                            channelId = logSetting.UserUnbannedId = (logSetting.UserUnbannedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.UserUpdated:
                            channelId = logSetting.UserUpdatedId = (logSetting.UserUpdatedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.UserMuted:
                            channelId = logSetting.UserMutedId = (logSetting.UserMutedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.ChannelCreated:
                            channelId = logSetting.ChannelCreatedId = (logSetting.ChannelCreatedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.ChannelDestroyed:
                            channelId = logSetting.ChannelDestroyedId = (logSetting.ChannelDestroyedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.ChannelUpdated:
                            channelId = logSetting.ChannelUpdatedId = (logSetting.ChannelUpdatedId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.UserPresence:
                            channelId = logSetting.LogUserPresenceId = (logSetting.LogUserPresenceId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.VoicePresence:
                            channelId = logSetting.LogVoicePresenceId = (logSetting.LogVoicePresenceId == null ? channel.Id : default(ulong?));
                            break;
                        case LogCommandService.LogType.VoicePresenceTTS:
                            channelId = logSetting.LogVoicePresenceTTSId = (logSetting.LogVoicePresenceTTSId == null ? channel.Id : default(ulong?));
                            break;
						case LogCommandService.LogType.VerificationSteps:
							channelId = logSetting.VerificationSteps = logSetting.VerificationSteps == null ? channel.Id : default(ulong?);
							break;
						case LogCommandService.LogType.VerificationMessages:
							channelId = logSetting.VerificationMessages = logSetting.VerificationMessages == null ? channel.Id : default(ulong?);
							break;
					}

                    await uow.SaveChangesAsync().ConfigureAwait(false);
                }

                if (channelId != null)
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