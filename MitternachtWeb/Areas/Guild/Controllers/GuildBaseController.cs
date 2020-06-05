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
		public bool PermissionReadGuildConfig  => HasPermission(BotLevelPermission.ReadAllGuildConfigs, GuildLevelPermission.ReadGuildConfig);
		[ViewData]
		public bool PermissionWriteGuildConfig => HasPermission(BotLevelPermission.WriteAllGuildConfigs, GuildLevelPermission.WriteGuildConfig);
		[ViewData]
		public bool PermissionReadMutes        => HasPermission(BotLevelPermission.ReadAllMutes, GuildLevelPermission.ReadMutes);
		[ViewData]
		public bool PermissionWriteMutes       => HasPermission(BotLevelPermission.WriteAllMutes, GuildLevelPermission.WriteMutes);
		[ViewData]
		public bool PermissionReadWarns        => HasPermission(BotLevelPermission.ReadAllWarns, GuildLevelPermission.ReadWarns);
		[ViewData]
		public bool PermissionForgiveWarns     => HasPermission(BotLevelPermission.ForgiveAllWarns, GuildLevelPermission.ForgiveWarns);
		[ViewData]
		public bool PermissionWriteWarns       => HasPermission(BotLevelPermission.WriteAllWarns, GuildLevelPermission.WriteWarns);
		[ViewData]
		public bool PermissionReadQuotes       => HasPermission(BotLevelPermission.ReadAllQuotes, GuildLevelPermission.ReadQuotes);
		[ViewData]
		public bool PermissionWriteQuotes      => HasPermission(BotLevelPermission.WriteAllQuotes, GuildLevelPermission.WriteQuotes);

		public override void OnActionExecuting(ActionExecutingContext context) {
			if(RouteData.Values.TryGetValue("guildId", out var guildIdString)) {
				GuildId = ulong.Parse(guildIdString.ToString());

				if(!ReadableGuilds.Contains(GuildId)) {
					if(Program.MitternachtBot.Client.Guilds.Any(sg => sg.Id == GuildId)) {
						context.Result = Unauthorized();
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
