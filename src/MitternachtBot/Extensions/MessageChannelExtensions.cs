using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Mitternacht.Extensions {
	public static class MessageChannelExtensions {
		public static Task<IUserMessage> EmbedAsync(this IMessageChannel ch, EmbedBuilder embed, string msg = "")
			=> ch.SendMessageAsync(msg, embed: embed.Build());

		public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string text, string title = null, string url = null, string footer = null)
			=> ch.EmbedAsync(GetSimpleEmbedBuilder(text, title, url, footer).WithErrorColor());

		public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, string text, string title = null, string url = null, string footer = null)
			=> ch.EmbedAsync(GetSimpleEmbedBuilder(text, title, url, footer).WithOkColor());

		private static EmbedBuilder GetSimpleEmbedBuilder(string description, string title, string url, string footer) {
			var eb = new EmbedBuilder().WithDescription(description);

			if(!string.IsNullOrWhiteSpace(title))
				eb.WithTitle(title);
			if(url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
				eb.WithUrl(url);
			if(!string.IsNullOrWhiteSpace(footer))
				eb.WithFooter(efb => efb.WithText(footer));

			return eb;
		}

		public static Task<IUserMessage> SendTableAsync<T>(this IMessageChannel ch, string seed, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3) {
			var i = 0;
			return ch.SendMessageAsync($"{seed}```css\n{string.Join("\n", items.GroupBy(item => i++ / columns).Select(ig => string.Concat(ig.Select(howToPrint))))}```");
		}

		private static readonly IEmote ArrowLeft          = new Emoji("⬅");
		private static readonly IEmote ArrowRight         = new Emoji("➡");
		private const           int    ReactionStartDelay = 500;
		private const           int    ReactionTime       = 30000;

		/// <summary>
		/// Creates a paginated confirm embed.
		/// </summary>
		/// <param name="channel">Channel to embed in.</param>
		/// <param name="client">DiscordSocketClient instance</param>
		/// <param name="currentPage">current page from 0 to lastpage</param>
		/// <param name="pageFunc">Func returning EmbedBuilder for each page</param>
		/// <param name="lastPage">Last page number, 0 based.</param>
		/// <param name="addPaginatedFooter">whether footer with current page numbers should be added or not</param>
		/// <param name="reactUsers">additional users which can change pages</param>
		/// <param name="hasPerms">overturn reactUsers if certain permission is available</param>
		/// <returns></returns>
		public static Task SendPaginatedConfirmAsync(this IMessageChannel channel, DiscordSocketClient client, int currentPage, Func<int, EmbedBuilder> pageFunc, int? lastPage = null, bool addPaginatedFooter = true, IGuildUser[] reactUsers = null, Func<GuildPermissions, bool> hasPerms = null)
			=> channel.SendPaginatedConfirmAsync(client, currentPage, x => Task.FromResult(pageFunc(x)), lastPage, addPaginatedFooter, reactUsers, hasPerms);

		/// <summary>
		/// Creates a paginated confirm embed.
		/// </summary>
		/// <param name="channel">Channel to embed in.</param>
		/// <param name="client">DiscordSocketClient instance</param>
		/// <param name="currentPage">current page from 0 to lastpage</param>
		/// <param name="pageFunc">Func returning EmbedBuilder for each page</param>
		/// <param name="lastPage">Last page number, 0 based.</param>
		/// <param name="addPaginatedFooter">whether footer with current page numbers should be added or not</param>
		/// <param name="reactUsers">additional users which can change pages</param>
		/// <param name="hasPerms">overturn reactUsers if certain permission is available</param>
		/// <returns></returns>
		public static async Task SendPaginatedConfirmAsync(this IMessageChannel channel, DiscordSocketClient client, int currentPage, Func<int, Task<EmbedBuilder>> pageFunc, int? lastPage = null, bool addPaginatedFooter = true, IGuildUser[] reactUsers = null, Func<GuildPermissions, bool> hasPerms = null) {
			reactUsers ??= new IGuildUser[0];
			if(hasPerms == null)
				hasPerms = gp => !reactUsers.Any();

			var embed = await pageFunc(currentPage).ConfigureAwait(false);

			if(addPaginatedFooter)
				embed.AddPaginatedFooter(currentPage, lastPage);

			var msg = await channel.EmbedAsync(embed);

			if(lastPage == 0)
				return;

			var _ = Task.Run(async () => {
				await msg.AddReactionAsync(ArrowLeft).ConfigureAwait(false);
				await msg.AddReactionAsync(ArrowRight).ConfigureAwait(false);

				await Task.Delay(ReactionStartDelay).ConfigureAwait(false);

				async void ChangePage(SocketReaction r) {
					try {
						if(!r.User.IsSpecified || r.User.Value is IGuildUser gu && reactUsers.All(u => u.Id != r.UserId) && !hasPerms.Invoke(gu.GuildPermissions) && !gu.GuildPermissions.Administrator)
							return;

						if(r.Emote.Name == ArrowLeft.Name) {
							if(currentPage == 0)
								return;
							var toSend = await pageFunc(--currentPage).ConfigureAwait(false);
							if(addPaginatedFooter)
								toSend.AddPaginatedFooter(currentPage, lastPage);
							await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
						} else if(r.Emote.Name == ArrowRight.Name) {
							if(lastPage != null && !(lastPage > currentPage))
								return;
							var toSend = await pageFunc(++currentPage).ConfigureAwait(false);
							if(addPaginatedFooter)
								toSend.AddPaginatedFooter(currentPage, lastPage);
							await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
						}
					} catch(Exception) {
						//ignored
					}
				}

				using(msg.OnReaction(client, ChangePage, ChangePage)) {
					await Task.Delay(ReactionTime).ConfigureAwait(false);
				}

				await msg.RemoveAllReactionsAsync().ConfigureAwait(false);
			});
		}
	}
}
