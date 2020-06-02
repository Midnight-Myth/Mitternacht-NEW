using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using NLog;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Forum.Services {
	public class ForumNotificationService : IMService {
		private readonly StringService _ss;

		public ForumNotificationService(DiscordSocketClient client, ForumService fs, DbService db, StringService ss) {
			_ss = ss;

			Task.Run(async () => {
				while(fs.Forum == null)
					await Task.Delay(TimeConstants.WaitForForum);

				var log = LogManager.GetCurrentClassLogger();
				var previousNotificationText = "";

				try {
					previousNotificationText = await fs.Forum.GetNotificationText().ConfigureAwait(false);
				} catch { }

				while(true) {
					try {
						var text = await fs.Forum.GetNotificationText().ConfigureAwait(false);
						
						if(!string.IsNullOrWhiteSpace(text) && text != previousNotificationText) {
							using var uow = db.UnitOfWork;
							foreach(var gc in uow.GuildConfigs.GetAllGuildConfigs(client.Guilds.Select(g => g.Id).ToList()).Where(gc => gc.ForumNotificationChannelId.HasValue)) {
								var channel = client.GetGuild(gc.GuildId).GetTextChannel(gc.ForumNotificationChannelId.Value);

								if(channel != null) {
									await channel.SendMessageAsync(_ss.GetText("forum", "forum_notification", gc.GuildId, text)).ConfigureAwait(false);
								}
							}
						}

						previousNotificationText = text;
					} catch(Exception e) {
						log.Warn(e, CultureInfo.CurrentCulture, "Failed to get or send forum notification.");
					}
					await Task.Delay(TimeConstants.ForumNotification);
				}
			});
		}

		private string GetText(string key, ulong guildId, params object[] replacements)
			=> _ss.GetText("forum", key, guildId, replacements);
	}
}
