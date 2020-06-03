using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MitternachtWeb.Controllers;
using MitternachtWeb.Exceptions;
using MitternachtWeb.Models;
using System;
using System.Linq;

namespace MitternachtWeb.Areas.Guild.Controllers {
	public abstract class GuildBaseController : DiscordUserController {
		[ViewData]
		public ulong       GuildId { get; private set; }
		[ViewData]
		public SocketGuild Guild   { get; private set; }

		[ViewData]
		public bool PermissionReadModeration   => DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.ReadAllModerations) || DiscordUser.GuildPagePermissions[GuildId].HasFlag(GuildLevelPermission.ReadModeration);
		[ViewData]
		public bool PermissionWriteMutes       => DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.WriteAllMutes) || DiscordUser.GuildPagePermissions[GuildId].HasFlag(GuildLevelPermission.WriteMutes);
		[ViewData]
		public bool PermissionReadGuildConfig  => DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.ReadAllGuildConfigs) || DiscordUser.GuildPagePermissions[GuildId].HasFlag(GuildLevelPermission.ReadGuildConfig);
		[ViewData]
		public bool PermissionWriteGuildConfig => DiscordUser.BotPagePermissions.HasFlag(BotLevelPermission.WriteAllGuildConfigs) || DiscordUser.GuildPagePermissions[GuildId].HasFlag(GuildLevelPermission.WriteGuildConfig);

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
	}
}
