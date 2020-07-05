using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Administration.Services {
	public class VoicePlusTextService : IMService {
		private readonly DiscordSocketClient _client;
		private readonly StringService _strings;
		private readonly DbService _db;
		private readonly Logger _log;

		private readonly Regex _channelNameRegex = new Regex(@"[^a-zA-Z0-9 -]", RegexOptions.Compiled);

		private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _guildLockObjects = new ConcurrentDictionary<ulong, SemaphoreSlim>();

		public VoicePlusTextService(DiscordSocketClient client, StringService strings, DbService db) {
			_client = client;
			_strings = strings;
			_db = db;
			_log = LogManager.GetCurrentClassLogger();

			_client.UserVoiceStateUpdated += UserUpdatedEventHandler;
		}

		private Task UserUpdatedEventHandler(SocketUser user, SocketVoiceState before, SocketVoiceState after) {
			if(!(user is SocketGuildUser guildUser))
				return Task.CompletedTask;

			var botUserPerms = guildUser.Guild.CurrentUser.GuildPermissions;

			if(before.VoiceChannel == after.VoiceChannel)
				return Task.CompletedTask;

			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildUser.Guild.Id);

			if(!gc.VoicePlusTextEnabled)
				return Task.CompletedTask;

			var _ = Task.Run(async () => {
				try {
					if(!botUserPerms.ManageChannels || !botUserPerms.ManageRoles) {
						try {
							await guildUser.Guild.Owner.SendErrorAsync(_strings.GetText("administration", "vt_exit", guildUser.Guild.Id, Format.Bold(guildUser.Guild.Name))).ConfigureAwait(false);
						} catch { }

						gc.VoicePlusTextEnabled = false;

						await uow.SaveChangesAsync().ConfigureAwait(false);
					} else {
						var semaphore = _guildLockObjects.GetOrAdd(guildUser.Guild.Id, (key) => new SemaphoreSlim(1, 1));

						try {
							await semaphore.WaitAsync().ConfigureAwait(false);

							var beforeVch = before.VoiceChannel;
							if(beforeVch != null) {
								var beforeRoleName = GetRoleName(beforeVch);
								var beforeRole = guildUser.Guild.Roles.FirstOrDefault(x => x.Name.Equals(beforeRoleName, StringComparison.OrdinalIgnoreCase));
								if(beforeRole != null) {
									_log.Info($"Removing role {beforeRoleName} from user {guildUser.Username}");
									await guildUser.RemoveRoleAsync(beforeRole).ConfigureAwait(false);
									await Task.Delay(200).ConfigureAwait(false);
								}
							}
							
							var afterVch = after.VoiceChannel;
							if(afterVch != null && guildUser.Guild.AFKChannel?.Id != afterVch.Id) {
								var roleToAdd = guildUser.Guild.Roles.FirstOrDefault(x => x.Name.Equals(GetRoleName(afterVch), StringComparison.OrdinalIgnoreCase));

								if(roleToAdd != null){
									_log.Info($"Adding role {roleToAdd.Name} to user {guildUser.Username}");
									await guildUser.AddRoleAsync(roleToAdd).ConfigureAwait(false);
								}
							}
						} finally {
							semaphore.Release();
						}
					}
				} catch (Exception ex) {
					_log.Warn(ex);
				}
			});
			return Task.CompletedTask;
		}

		public string GetChannelName(string voiceName)
			=> $"{_channelNameRegex.Replace(voiceName, "").Trim().Replace(" ", "-").TrimTo(90, true)}-voice";

		public string GetRoleName(IVoiceChannel ch)
			=> $"nvoice-{ch.Id}";
	}
}
