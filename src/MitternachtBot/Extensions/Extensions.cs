using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SixLabors.ImageSharp;
using Mitternacht.Common.Collections;
using Mitternacht.Services.Discord;
using Newtonsoft.Json;
using SixLabors.ImageSharp.PixelFormats;

namespace Mitternacht.Extensions {
	public static class Extensions {
		public static async Task<string> ReplaceAsync(this Regex regex, string input, Func<Match, Task<string>> replacementFn) {
			var sb = new StringBuilder();
			var lastIndex = 0;

			foreach(Match match in regex.Matches(input)) {
				sb.Append(input, lastIndex, match.Index - lastIndex)
				  .Append(await replacementFn(match).ConfigureAwait(false));

				lastIndex = match.Index + match.Length;
			}

			sb.Append(input, lastIndex, input.Length - lastIndex);
			return sb.ToString();
		}

		public static void ThrowIfNull<T>(this T obj, string name) where T : class {
			if(obj == null)
				throw new ArgumentNullException(nameof(name));
		}

		public static ConcurrentDictionary<TKey, TValue> ToConcurrent<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dict)
			=> new ConcurrentDictionary<TKey, TValue>(dict);

		public static bool IsAuthor(this IMessage msg, IDiscordClient client) =>
			msg.Author?.Id == client.CurrentUser.Id;

		public static string RealSummary(this CommandInfo cmd, string prefix) => string.Format(cmd.Summary, prefix);
		public static string RealRemarks(this CommandInfo cmd, string prefix) => string.Format(cmd.Remarks, prefix);

		public static EmbedBuilder AddPaginatedFooter(this EmbedBuilder embed, int curPage, int? lastPage) {
			return lastPage != null ? embed.WithFooter(efb => efb.WithText($"{curPage + 1} / {lastPage + 1}")) : embed.WithFooter(efb => efb.WithText(curPage.ToString()));
		}

		public static EmbedBuilder WithOkColor(this EmbedBuilder eb) =>
			eb.WithColor(MitternachtBot.OkColor);

		public static EmbedBuilder WithErrorColor(this EmbedBuilder eb) =>
			eb.WithColor(MitternachtBot.ErrorColor);

		public static ReactionEventWrapper OnReaction(this IUserMessage msg, DiscordSocketClient client, Action<SocketReaction> reactionAdded, Action<SocketReaction> reactionRemoved = null) {
			if(reactionRemoved == null)
				reactionRemoved = delegate { };

			var wrap = new ReactionEventWrapper(client, msg);
			wrap.OnReactionAdded += r => { var _ = Task.Run(() => reactionAdded(r)); };
			wrap.OnReactionRemoved += r => { var _ = Task.Run(() => reactionRemoved(r)); };
			return wrap;
		}

		public static void AddFakeHeaders(this HttpClient http) {
			http.DefaultRequestHeaders.Clear();
			http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1");
			http.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
		}

		public static IMessage DeleteAfter(this IUserMessage msg, int seconds) {
			Task.Run(async () => {
				await Task.Delay(seconds * 1000);
				try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
			});
			return msg;
		}

		public static ModuleInfo GetTopLevelModule(this ModuleInfo module) {
			while(module.Parent != null) {
				module = module.Parent;
			}
			return module;
		}

		//public static async Task<IEnumerable<IGuildUser>> MentionedUsers(this IUserMessage msg) =>


		public static void AddRange<T>(this HashSet<T> target, IEnumerable<T> elements) where T : class {
			foreach(var item in elements) {
				target.Add(item);
			}
		}

		public static void AddRange<T>(this ConcurrentHashSet<T> target, IEnumerable<T> elements) where T : class {
			foreach(var item in elements) {
				target.Add(item);
			}
		}

		public static double UnixTimestamp(this DateTime dt) => dt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

		public static async Task<IEnumerable<IGuildUser>> GetMembersAsync(this IRole role) =>
			(await role.Guild.GetUsersAsync(CacheMode.CacheOnly)).Where(u => u.RoleIds.Contains(role.Id));

		public static string ToJson<T>(this T any, Formatting formatting = Formatting.Indented) =>
			JsonConvert.SerializeObject(any, formatting);

		public static Stream ToStream(this Image<Rgba32> img) {
			var imageStream = new MemoryStream();
			img.SaveAsPng(imageStream);
			imageStream.Position = 0;
			return imageStream;
		}

		/// <summary>
		/// returns an IEnumerable with randomized element order
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> items) {
			// Thanks to @Joe4Evr for finding a bug in the old version of the shuffle
			using(var provider = RandomNumberGenerator.Create()) {
				var list = items.ToList();
				var n = list.Count;
				while(n > 1) {
					var box = new byte[n / byte.MaxValue + 1];
					int boxSum;
					do {
						provider.GetBytes(box);
						boxSum = box.Sum(b => b);
					}
					while(!(boxSum < n * (byte.MaxValue * box.Length / n)));
					var k = boxSum % n;
					n--;
					var value = list[k];
					list[k] = list[n];
					list[n] = value;
				}
				return list;
			}
		}

		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> elems, Action<T> exec) {
			foreach(var elem in elems) {
				exec(elem);
			}
			return elems;
		}

		public static Stream ToStream(this IEnumerable<byte> bytes, bool canWrite = false) {
			var ms = new MemoryStream(bytes as byte[] ?? bytes.ToArray(), canWrite);
			ms.Seek(0, SeekOrigin.Begin);
			return ms;
		}

		public static IEnumerable<IRole> GetRoles(this IGuildUser user) =>
			user.RoleIds.Select(r => user.Guild.GetRole(r)).Where(r => r != null);

		public static async Task<IMessage> SendMessageToOwnerAsync(this IGuild guild, string message) {
			var ownerPrivate = await (await guild.GetOwnerAsync().ConfigureAwait(false)).GetOrCreateDMChannelAsync()
								.ConfigureAwait(false);

			return await ownerPrivate.SendMessageAsync(message).ConfigureAwait(false);
		}

		public static Image<Rgba32> Merge(this IEnumerable<Image<Rgba32>> images) {
			var imgs = images.ToArray();

			var canvas = new Image<Rgba32>(imgs.Sum(img => img.Width), imgs.Max(img => img.Height));

			//var xOffset = 0;
			//foreach (var t in imgs) {
			//    canvas.DrawImage(t, 100, default(Size), new Point(xOffset, 0));
			//    xOffset += t.Bounds.Width;
			//}

			return canvas;
		}

		//modified code taken from Discord.Net.Commands.UserTypeReader 
		public static async Task<IGuildUser> GetUserAsync(this IGuild guild, string input) {
			var guildUsers = await guild.GetUsersAsync().ConfigureAwait(false);
			IGuildUser user;

			if(MentionUtils.TryParseUser(input, out var id)) {
				user = await guild.GetUserAsync(id).ConfigureAwait(false);
				if(user != null)
					return user;
			}

			if(ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out id)) {
				user = await guild.GetUserAsync(id).ConfigureAwait(false);
				if(user != null)
					return user;
			}

			var index = input.LastIndexOf('#');
			if(index >= 0) {
				var username = input.Substring(0, index);
				if(ushort.TryParse(input.Substring(index + 1), out var discrim)) {
					user = guildUsers.FirstOrDefault(x => x.DiscriminatorValue == discrim && string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase));
					if(user != null)
						return user;
				}
			}

			user = guildUsers.FirstOrDefault(gu => string.Equals(input, gu.Username, StringComparison.OrdinalIgnoreCase));
			if(user != null)
				return user;

			user = guildUsers.FirstOrDefault(gu => string.Equals(input, gu.Nickname, StringComparison.OrdinalIgnoreCase));
			return user;
		}

		public static bool IsOtherDate(this DateTime date, DateTime other, bool ignoreYear = false)
			=> (!ignoreYear && date.Year != other.Year) || date.Month != other.Month || date.Day != other.Day;

		/// <summary>
		/// Returns the name of a Module and cuts the end 'Commands' of, if any.
		/// </summary>
		/// <param name="mi">ModuleInfo of Module</param>
		/// <returns>Module name</returns>
		public static string GetModuleName(this ModuleInfo mi)
			=> !mi.IsSubmodule ? mi.Name : (mi.Name.EndsWith("commands", StringComparison.OrdinalIgnoreCase) && mi.Name.Length > 8 ? mi.Name[0..^8] : mi.Name);

		public static async Task<ulong[]> GetUserIdsAsync(this IGuild guild) {
			var guildUsers = await guild.GetUsersAsync();
			return guildUsers.Select(gu => gu.Id).ToArray();
		}
	}
}