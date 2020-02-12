using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Mitternacht.Common.Collections;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using NLog;

namespace Mitternacht.Modules.Administration.Services
{
    public class GameVoiceChannelService : IMService
    {
        public readonly ConcurrentHashSet<ulong> GameVoiceChannels;

        private readonly Logger _log;

        public GameVoiceChannelService(DiscordSocketClient client, IEnumerable<GuildConfig> gcs)
        {
            _log = LogManager.GetCurrentClassLogger();

            GameVoiceChannels = new ConcurrentHashSet<ulong>(
                gcs.Where(gc => gc.GameVoiceChannel != null)
                                         .Select(gc => gc.GameVoiceChannel.Value));

            client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;

        }

        private Task Client_UserVoiceStateUpdated(SocketUser usr, SocketVoiceState oldState, SocketVoiceState newState)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!(usr is SocketGuildUser gUser))
                        return;

                    var game = gUser.Activity?.Name?.TrimTo(50).ToLowerInvariant();

                    if (oldState.VoiceChannel == newState.VoiceChannel ||
                        newState.VoiceChannel == null)
                        return;

                    if (!GameVoiceChannels.Contains(newState.VoiceChannel.Id) ||
                        string.IsNullOrWhiteSpace(game))
                        return;

                    var vch = gUser.Guild.VoiceChannels
                        .FirstOrDefault(x => x.Name.ToLowerInvariant() == game);

                    if (vch == null)
                        return;

                    await Task.Delay(1000).ConfigureAwait(false);
                    await gUser.ModifyAsync(gu => gu.Channel = vch).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            });

            return Task.CompletedTask;
        }
    }
}
