using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Administration.Services {
	public class VcRoleService : IMService {
		private readonly DbService _db;
		private readonly Logger _log;

		public VcRoleService(DiscordSocketClient client, DbService db) {
			_db = db;
			_log = LogManager.GetCurrentClassLogger();

			client.UserVoiceStateUpdated += ClientOnUserVoiceStateUpdated;
		}

		private Task ClientOnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState) {
			if(!(user is SocketGuildUser guildUser))
				return Task.CompletedTask;

			var oldVoiceChannel = oldState.VoiceChannel;
			var newVoiceChannel = newState.VoiceChannel;
			var _ = Task.Run(async () => {
				try {
					if(oldVoiceChannel != newVoiceChannel) {
						using var uow = _db.UnitOfWork;
						var voiceChannelRoleInfos = uow.GuildConfigs.For(guildUser.Guild.Id, set => set.Include(x => x.VcRoleInfos)).VcRoleInfos;
						var oldVoiceChannelRoleInfo = voiceChannelRoleInfos.FirstOrDefault(vcri => vcri.VoiceChannelId == oldVoiceChannel?.Id);
						var oldVoiceChannelRole = guildUser.Roles.FirstOrDefault(r => r.Id == oldVoiceChannelRoleInfo?.RoleId);

						if(oldVoiceChannelRole != null) {
							try {
								await guildUser.RemoveRoleAsync(oldVoiceChannelRole).ConfigureAwait(false);
								await Task.Delay(200).ConfigureAwait(false);
							} catch {
								await Task.Delay(200).ConfigureAwait(false);
								await guildUser.RemoveRoleAsync(oldVoiceChannelRole).ConfigureAwait(false);
								await Task.Delay(200).ConfigureAwait(false);
							}
						}

						var newVoiceChannelRoleInfo = voiceChannelRoleInfos.FirstOrDefault(vcri => vcri.VoiceChannelId == newVoiceChannel?.Id);
						if(newVoiceChannelRoleInfo != null){
							var newVoiceChannelRole = guildUser.Guild.GetRole(newVoiceChannelRoleInfo.RoleId);

							if(newVoiceChannelRole != null){
								await guildUser.AddRoleAsync(newVoiceChannelRole).ConfigureAwait(false);
							}
						}
					}
				} catch (Exception ex) {
					_log.Warn(ex);
				}
			});
			return Task.CompletedTask;
		}
	}
}
