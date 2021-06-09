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

		private static readonly Dictionary<ulong, (DateTime RequestTime, DiscordUser User)> DiscordUsers = new();
		private static readonly Dictionary<ulong, (DateTime RequestTime, ulong[] Guilds  )> UserGuilds   = new();

		private static readonly HttpClient HttpClient = new();

		public static async Task<DiscordUser> GetDiscordUserAsync(ClaimsPrincipal user, HttpContext context) {
			var userIdString = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
			if(ulong.TryParse(userIdString, out var userId)) {
				var success = DiscordUsers.TryGetValue(userId, out var t);

				if(success && (t.RequestTime - DateTime.UtcNow).TotalSeconds < DiscordUserCacheTime) {
					return t.User;
				} else {
					var dUser          = Program.MitternachtBot.Client.GetUser(userId);
					var botPagePerms   = Program.MitternachtBot.Credentials.IsOwner(dUser) ? BotLevelPermission.All : BotLevelPermission.None;
					var guilds         = await GetKnownDiscordUserGuildsAsync(user, context);
					var guildPagePerms = guilds?.Select(id => Program.MitternachtBot.Client.Guilds.FirstOrDefault(g => g.Id == id)?.GetUser(userId)).Where(gu => gu != null).ToDictionary(gu => gu.Guild.Id, gu => gu.GuildPermissions.GetGuildLevelPermissions());

					var discordUser = new DiscordUser {
						User                 = dUser,
						BotPagePermissions   = botPagePerms,
						GuildPagePermissions = guildPagePerms ?? new Dictionary<ulong, GuildLevelPermission>()
					};
					DiscordUsers.Add(userId, (DateTime.UtcNow, discordUser));
					return discordUser;
				}
			} else {
				return null;
			}
		}

		public static async Task<ulong[]> GetKnownDiscordUserGuildsAsync(ClaimsPrincipal user, HttpContext context) {
			var userIdString = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

			if(ulong.TryParse(userIdString, out var userId)) {
				var success = UserGuilds.TryGetValue(userId, out var t);

				if(success && (t.RequestTime - DateTime.UtcNow).TotalSeconds < DiscordUserGuildsCacheTime) {
					return t.Guilds;
				} else {
					var request = new HttpRequestMessage(HttpMethod.Get, "https://discordapp.com/api/users/@me/guilds");
					request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await context.GetTokenAsync("access_token"));
					request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

					var response = await HttpClient.SendAsync(request);

					if(response.IsSuccessStatusCode) {
						var content = JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync());
						var botGuilds = Program.MitternachtBot.Client.Guilds.Select(g => g.Id).ToArray();
						var guilds = content.Select(o => o.Value<ulong>("id")).Intersect(botGuilds).Distinct().ToArray();

						UserGuilds.Add(userId, (DateTime.UtcNow, guilds));
						return guilds;
					} else {
						return null;
					}
				}
			} else {
				return null;
			}
		}

		public static void RemoveCache(ulong userId) {
			DiscordUsers.Remove(userId);
			UserGuilds.Remove(userId);
		}
	}
}
