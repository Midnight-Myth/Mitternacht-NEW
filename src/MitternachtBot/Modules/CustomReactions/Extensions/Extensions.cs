using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Dom;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Common.Replacements;
using Mitternacht.Extensions;
using Mitternacht.Modules.CustomReactions.Services;
using Mitternacht.Services.Database.Models;
using MoreLinq;

namespace Mitternacht.Modules.CustomReactions.Extensions {
	public static class Extensions {
		private static readonly Regex ImgRegex = new Regex("%(img|image):(?<tag>.*?)%", RegexOptions.Compiled);

		public static Dictionary<Regex, Func<Match, Task<string>>> RegexPlaceholders = new Dictionary<Regex, Func<Match, Task<string>>>() {
			{
				ImgRegex, async match => {
					var tag = match.Groups["tag"].ToString();
					if(string.IsNullOrWhiteSpace(tag))
						return "";

					var fullQueryLink = $"http://imgur.com/search?q={tag}";
					var config = Configuration.Default.WithDefaultLoader();
					var document = await BrowsingContext.New(config).OpenAsync(fullQueryLink);

					var elems = document.QuerySelectorAll("a.image-list-link").ToArray();

					return !elems.Any() ? "" : elems.RandomSubset(1).FirstOrDefault().Children?.FirstOrDefault() is IHtmlImageElement img && img.Source != null ? $" {img.Source.Replace("b.", ".")} " : ""; }
			}
		};

		private static string ResolveTriggerString(this CustomReaction cr, IUserMessage ctx, DiscordSocketClient client) {
			var rep = new ReplacementBuilder().WithUser(ctx.Author).WithClient(client).Build();

			var str = rep.Replace(cr.Trigger.ToLowerInvariant());

			return str;
		}

		private static async Task<string> ResolveResponseStringAsync(this string str, IUserMessage ctx, DiscordSocketClient client, string resolvedTrigger, bool containsAnywhere) {
			var substringIndex = resolvedTrigger.Length;
			if(containsAnywhere) {
				var pos = ctx.Content.GetWordPosition(resolvedTrigger);
				if(pos == WordPosition.Start)
					substringIndex += 1;
				else if(pos == WordPosition.End)
					substringIndex = ctx.Content.Length;
				else if(pos == WordPosition.Middle)
					substringIndex += ctx.Content.IndexOf(resolvedTrigger, StringComparison.Ordinal);
			}

			var rep = new ReplacementBuilder()
				.WithDefault(ctx.Author, ctx.Channel, (ctx.Channel as ITextChannel)?.Guild, client)
				.WithOverride("%target%", () => ctx.Content.Substring(substringIndex).Trim())
				.Build();

			str = rep.Replace(str);

			foreach(var ph in RegexPlaceholders) {
				str = await ph.Key.ReplaceAsync(str, ph.Value);
			}
			return str;
		}

		public static string TriggerWithContext(this CustomReaction cr, IUserMessage ctx, DiscordSocketClient client)
			=> cr.ResolveTriggerString(ctx, client);

		public static Task<string> ResponseWithContextAsync(this CustomReaction cr, IUserMessage ctx, DiscordSocketClient client, bool containsAnywhere)
			=> cr.Response.ResolveResponseStringAsync(ctx, client, cr.ResolveTriggerString(ctx, client), containsAnywhere);

		public static async Task<IUserMessage> Send(this CustomReaction cr, IUserMessage ctx, DiscordSocketClient client, CustomReactionsService crs) {
			var channel = cr.DmResponse ? await ctx.Author.GetOrCreateDMChannelAsync() : ctx.Channel;

			crs.ReactionStats.AddOrUpdate(cr.Trigger, 1, (k, old) => ++old);

			if(!CREmbed.TryParse(cr.Response, out var crembed))
				return await channel.SendMessageAsync((await cr.ResponseWithContextAsync(ctx, client, cr.ContainsAnywhere)).SanitizeMentions());
			var trigger = cr.ResolveTriggerString(ctx, client);
			var substringIndex = trigger.Length;
			if(cr.ContainsAnywhere) {
				var pos = ctx.Content.GetWordPosition(trigger);
				if(pos == WordPosition.Start)
					substringIndex += 1;
				else if(pos == WordPosition.End) {
					substringIndex = ctx.Content.Length;
				} else if(pos == WordPosition.Middle) {
					substringIndex += ctx.Content.IndexOf(trigger, StringComparison.Ordinal);
				}
			}

			var rep = new ReplacementBuilder()
				.WithDefault(ctx.Author, ctx.Channel, (ctx.Channel as ITextChannel)?.Guild, client)
				.WithOverride("%target%", () => ctx.Content.Substring(substringIndex).Trim())
				.Build();

			rep.Replace(crembed);

			return await channel.EmbedAsync(crembed.ToEmbed(), crembed.PlainText?.SanitizeMentions() ?? "");
		}

		public static WordPosition GetWordPosition(this string str, string word)
			=> str.StartsWith($"{word} ")
				? WordPosition.Start
				: str.EndsWith($" {word}")
				? WordPosition.End
				: str.Contains($" {word} ")
				? WordPosition.Middle
				: WordPosition.None;
	}

	public enum WordPosition {
		None,
		Start,
		Middle,
		End
	}
}