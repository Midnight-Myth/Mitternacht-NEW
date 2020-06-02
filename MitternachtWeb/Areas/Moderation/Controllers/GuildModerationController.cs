using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MitternachtWeb.Controllers;
using MitternachtWeb.Exceptions;
using MitternachtWeb.Models;
using System;
using System.Linq;

namespace MitternachtWeb.Areas.Moderation.Controllers {
	public abstract class GuildModerationController : DiscordUserController {
		[ViewData]
		public ulong       GuildId { get; private set; }
		[ViewData]
		public SocketGuild Guild   { get; private set; }

		protected ulong[] ReadableGuilds => DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.ReadAllModerations) ? DiscordUser.GuildPagePermissions.Select(kv => kv.Key).ToArray() : DiscordUser.GuildPagePermissions.Where(kv => kv.Value.HasFlag(GuildLevelPermission.ReadModeration)).Select(kv => kv.Key).ToArray();

		public override void OnActionExecuting(ActionExecutingContext context) {
			if(RouteData.Values.TryGetValue("guildId", out var guildIdString)) {
				GuildId = ulong.Parse(guildIdString.ToString());

				if(!ReadableGuilds.Contains(GuildId)) {
					if(Program.MitternachtBot.Client.Guilds.Any(sg => sg.Id == GuildId)) {
						throw new NoPermissionsException();
					} else {
						throw new GuildNotFoundException(GuildId);
					}
				} else {
					Guild = Program.MitternachtBot.Client.GetGuild(GuildId);
				}
			} else {
				throw new ArgumentNullException("guildId");
			}

			base.OnActionExecuting(context);
		}

		protected bool HasReadPermission(ulong guildId)
			=> DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.ReadAllGuildConfigs) || DiscordUser.GuildPagePermissions[guildId].HasFlag(GuildLevelPermission.ReadModeration);

		protected bool HasWritePermission(ulong guildId)
			=> DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.WriteAllGuildConfigs) || DiscordUser.GuildPagePermissions[guildId].HasFlag(GuildLevelPermission.WriteModeration);
	}
}
