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

		public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string title, string error, string url = null, string footer = null) {
			var eb = new EmbedBuilder().WithErrorColor().WithDescription(error)
										.WithTitle(title);
			if(url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
				eb.WithUrl(url);
			if(!string.IsNullOrWhiteSpace(footer))
				eb.WithFooter(efb => efb.WithText(footer));
			return ch.SendMessageAsync("", embed: eb.Build());
		}

		public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string error)
			=> ch.SendMessageAsync("", embed: new EmbedBuilder().WithErrorColor().WithDescription(error).Build());

		public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, string title, string text, string url = null, string footer = null) {
			var eb = new EmbedBuilder().WithOkColor().WithDescription(text).WithTitle(title);
			if(url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute)) eb.WithUrl(url);
			if(!string.IsNullOrWhiteSpace(footer)) eb.WithFooter(efb => efb.WithText(footer));
			return ch.SendMessageAsync("", embed: eb.Build());
		}

		public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, string text)
			=> ch.SendMessageAsync("", embed: new EmbedBuilder().WithOkColor().WithDescription(text).Build());

		public static Task<IUserMessage> SendTableAsync<T>(this IMessageChannel ch, string seed, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3) {
			var i = 0;
			return ch.SendMessageAsync($"{seed}```css\n{string.Join("\n", items.GroupBy(item => i++ / columns).Select(ig => string.Concat(ig.Select(howToPrint))))}```");
		}

		public static Task<IUserMessage> SendTableAsync<T>(this IMessageChannel ch, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3)
			=> ch.SendTableAsync("", items, howToPrint, columns);

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
			reactUsers??=new IGuildUser[0];
			if(hasPerms == null) hasPerms = gp => !reactUsers.Any();

			var embed = await pageFunc(currentPage).ConfigureAwait(false);

			if(addPaginatedFooter)
				embed.AddPaginatedFooter(currentPage, lastPage);

			var msg = await channel.EmbedAsync(embed);

			if(lastPage == 0) return;

			var _ = Task.Run(async () => {
				await msg.AddReactionAsync(ArrowLeft).ConfigureAwait(false);
				await msg.AddReactionAsync(ArrowRight).ConfigureAwait(false);

				await Task.Delay(ReactionStartDelay).ConfigureAwait(false);

				async void ChangePage(SocketReaction r) {
					try {
						if(!r.User.IsSpecified || r.User.Value is IGuildUser gu && reactUsers.All(u => u.Id != r.UserId) && !hasPerms.Invoke(gu.GuildPermissions) && !gu.GuildPermissions.Administrator) return;

						if(r.Emote.Name == ArrowLeft.Name) {
							if(currentPage == 0)
								return;
							var toSend = await pageFunc(--currentPage).ConfigureAwait(false);
							if(addPaginatedFooter)
								toSend.AddPaginatedFooter(currentPage, lastPage);
							await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
						} else if(r.Emote.Name == ArrowRight.Name) {
							if(lastPage != null && !(lastPage > currentPage)) return;
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

		public static Task SendPaginatedMessageAsync(this IMessageChannel channel, DiscordSocketClient client, int currentPage, Func<int, string> pageFunc, int? lastPage = null, bool addPaginatedFooter = true)
			=> channel.SendPaginatedMessageAsync(client, currentPage, p => Task.FromResult(pageFunc(p)), lastPage, addPaginatedFooter);

		public static async Task SendPaginatedMessageAsync(this IMessageChannel channel, DiscordSocketClient client, int currentPage, Func<int, Task<string>> pageFunc, int? lastPage = null, bool addPaginatedFooter = true) {
			var text = await pageFunc(currentPage);

			if(addPaginatedFooter)
				text = text.Replace("{page}", lastPage == null ? currentPage.ToString() : $"{currentPage}/{lastPage}");

			var msg = await channel.SendMessageAsync(text);
			if(lastPage == 0) return;

			var _ = Task.Run(async () => {
				await msg.AddReactionAsync(ArrowLeft);
				await msg.AddReactionAsync(ArrowRight);

				await Task.Delay(ReactionStartDelay);

				async void ChangePage(SocketReaction r) {
					try {
						if(r.Emote.Name == ArrowLeft.Name) {
							if(currentPage == 0) return;
							var modtext = await pageFunc(--currentPage);
							if(addPaginatedFooter)
								modtext += lastPage == null ? $"\n{currentPage}" : $"\n{currentPage}/{lastPage}";
							await msg.ModifyAsync(mp => mp.Content = modtext);
						} else if(r.Emote.Name == ArrowRight.Name) {
							if(lastPage != null && !(lastPage > currentPage)) return;
							var modtext = await pageFunc(++currentPage);
							if(addPaginatedFooter)
								modtext += lastPage == null ? $"\n{currentPage}" : $"\n{currentPage}/{lastPage}";
							await msg.ModifyAsync(mp => mp.Content = modtext);
						}
					} catch(Exception) {
						//who needs exception handling?
					}
				}

				using(msg.OnReaction(client, ChangePage, ChangePage)) {
					await Task.Delay(ReactionTime);
				}

				await msg.RemoveAllReactionsAsync();
			});
		}
	}
}
