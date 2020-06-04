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

		private bool HasPermission(BotLevelPermission botLevelPermission, GuildLevelPermission guildLevelPermission)
			=> DiscordUser.BotPagePermissions.HasFlag(botLevelPermission) || DiscordUser.GuildPagePermissions[GuildId].HasFlag(guildLevelPermission);

		[ViewData]
		public bool PermissionReadModeration   => HasPermission(BotLevelPermission.ReadAllModerations, GuildLevelPermission.ReadModeration);
		[ViewData]
		public bool PermissionWriteMutes       => HasPermission(BotLevelPermission.WriteAllMutes, GuildLevelPermission.WriteMutes);
		[ViewData]
		public bool PermissionReadGuildConfig  => HasPermission(BotLevelPermission.ReadAllGuildConfigs, GuildLevelPermission.ReadGuildConfig);
		[ViewData]
		public bool PermissionWriteGuildConfig => HasPermission(BotLevelPermission.WriteAllGuildConfigs, GuildLevelPermission.WriteGuildConfig);
		[ViewData]
		public bool PermissionForgiveWarns     => HasPermission(BotLevelPermission.ForgiveAllWarns, GuildLevelPermission.ForgiveWarns);
		[ViewData]
		public bool PermissionWriteWarns       => HasPermission(BotLevelPermission.WriteAllWarns, GuildLevelPermission.WriteWarns);

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
