using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using MitternachtWeb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MitternachtWeb.Helpers {
	public static class UserHelper {
		public const double DiscordUserCacheTime = 60.0;
		public const double DiscordUserGuildsCacheTime = 300.0;

		private static readonly Dictionary<ulong, (DateTime RequestTime, DiscordUser User)> discordUsers = new Dictionary<ulong, (DateTime, DiscordUser)>();
		private static readonly Dictionary<ulong, (DateTime RequestTime, ulong[] Guilds  )> userGuilds   = new Dictionary<ulong, (DateTime, ulong[]    )>();

		private static readonly HttpClient httpClient = new HttpClient();

		public static async Task<DiscordUser> GetDiscordUserAsync(ClaimsPrincipal user, HttpContext context) {
			var userIdString = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
			if(ulong.TryParse(userIdString, out var userId)) {
				var success = discordUsers.TryGetValue(userId, out var t);

				if(success && (t.RequestTime - DateTime.UtcNow).TotalSeconds < DiscordUserCacheTime) {
					return t.User;
				} else {
					var dUser          = Program.MitternachtBot.Client.GetUser(userId);
					var botPagePerms   = Program.MitternachtBot.Credentials.IsOwner(dUser) ? BotPagePermission.All : BotPagePermission.None;
					var guilds         = await GetKnownDiscordUserGuildsAsync(user, context);
					var guildPagePerms = guilds.Select(id => Program.MitternachtBot.Client.Guilds.FirstOrDefault(g => g.Id == id)?.GetUser(userId)).Where(gu => gu != null).ToDictionary(gu => gu.Guild.Id, gu => gu.GuildPermissions.GetGuildPagePermissions());

					var discordUser = new DiscordUser {
						User                 = dUser,
						BotPagePermissions   = botPagePerms,
						GuildPagePermissions = guildPagePerms
					};
					discordUsers.Add(userId, (DateTime.UtcNow, discordUser));
					return discordUser;
				}
			} else {
				return null;
			}
		}

		public static async Task<ulong[]> GetKnownDiscordUserGuildsAsync(ClaimsPrincipal user, HttpContext context) {
			var userIdString = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

			if(ulong.TryParse(userIdString, out var userId)) {
				var success = userGuilds.TryGetValue(userId, out var t);

				if(success && (t.RequestTime - DateTime.UtcNow).TotalSeconds < DiscordUserGuildsCacheTime) {
					return t.Guilds;
				} else {
					var request = new HttpRequestMessage(HttpMethod.Get, "https://discordapp.com/api/users/@me/guilds");
					request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await context.GetTokenAsync("access_token"));
					request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

					var response = await httpClient.SendAsync(request);

					if(response.IsSuccessStatusCode) {
						var content = JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync());
						var botGuilds = Program.MitternachtBot.Client.Guilds.Select(g => g.Id).ToArray();
						var guilds = content.Select(o => o.Value<ulong>("id")).Intersect(botGuilds).Distinct().ToArray();

						userGuilds.Add(userId, (DateTime.UtcNow, guilds));
						return guilds;
					} else {
						return null;
					}
				}
			} else {
				return null;
			}
		}
	}
}
