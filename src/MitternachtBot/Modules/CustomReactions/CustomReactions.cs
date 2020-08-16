using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.CustomReactions.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;
using Newtonsoft.Json;

namespace Mitternacht.Modules.CustomReactions {
	public class CustomReactions : MitternachtTopLevelModule<CustomReactionsService> {
		private readonly IBotCredentials _creds;
		private readonly IUnitOfWork uow;

		public CustomReactions(IBotCredentials creds, IUnitOfWork uow) {
			_creds = creds;
			this.uow = uow;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task AddCustReact(string key, [Remainder] string message) {
			var channel = Context.Channel as ITextChannel;
			if(string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
				return;

			if(channel == null && !_creds.IsOwner(Context.User) || channel != null && !((IGuildUser)Context.User).GuildPermissions.Administrator) {
				await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
				return;
			}

			var cr = new CustomReaction() {
				GuildId  = channel?.Guild.Id,
				IsRegex  = false,
				Trigger  = key,
				Response = message,
			};

			uow.CustomReactions.Add(cr);

			await uow.SaveChangesAsync(false).ConfigureAwait(false);

			var eb = new EmbedBuilder().WithOkColor()
				.WithTitle(GetText("new_cust_react"))
				.WithDescription($"#{cr.Id}")
				.AddField(efb => efb.WithName(GetText("trigger")).WithValue(key))
				.AddField(efb => efb.WithName(GetText("response")).WithValue(message.Length > 1024 ? GetText("redacted_too_long") : message));

			await Context.Channel.EmbedAsync(eb).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[Priority(1)]
		public async Task ListCustReact(int page = 1) {
			if(--page < 0 || page > 999)
				return;
			var customReactions = Context.Guild == null
				? Service.GlobalReactions
				: Service.ReactionsForGuild(Context.Guild.Id);

			if(!customReactions.Any()) {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
				return;
			}

			const int elementsPerPage = 20;
			await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage => new EmbedBuilder().WithOkColor().WithTitle(GetText("name")).WithDescription(string.Join("\n",
					customReactions.OrderBy(cr => cr.Trigger).Skip(currentPage * elementsPerPage).Take(elementsPerPage).Select(cr => {
						var str = $"`#{cr.Id}` {cr.Trigger}";
						if(cr.AutoDeleteTrigger) {
							str = "🗑" + str;
						}
						if(cr.DmResponse) {
							str = "📪" + str;
						}
						return str;
					}))), (int)Math.Ceiling(customReactions.Length * 1d / elementsPerPage), reactUsers: new[] { Context.User as IGuildUser }).ConfigureAwait(false);
		}

		public enum All {
			All
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[Priority(0)]
		public async Task ListCustReact(All x) {
			var customReactions = Context.Guild == null
				? Service.GlobalReactions
				: Service.ReactionsForGuild(Context.Guild.Id);

			if(!customReactions.Any()) {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
				return;
			}

			var txtStream = await JsonConvert.SerializeObject(customReactions.GroupBy(cr => cr.Trigger)
														.OrderBy(cr => cr.Key)
														.Select(cr => new { Trigger = cr.Key, Responses = cr.Select(y => new { id = y.Id, text = y.Response }).ToList() }), Formatting.Indented)
														.ToStream()
														.ConfigureAwait(false);

			var channel = Context.Guild == null ? Context.Channel : await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);
			
			await channel.SendFileAsync(txtStream, "customreactions.txt", GetText("list_all")).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task ListCustReactG(int page = 1) {
			if(--page < 0 || page > 9999)
				return;
			var customReactions = Context.Guild == null
				? Service.GlobalReactions
				: Service.ReactionsForGuild(Context.Guild.Id);

			if(!customReactions.Any()) {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
			} else {
				var ordered = customReactions
					.GroupBy(cr => cr.Trigger)
					.OrderBy(cr => cr.Key)
					.ToList();

				const int elementsPerPage = 20;
				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage =>
					new EmbedBuilder().WithOkColor()
						.WithTitle(GetText("name"))
						.WithDescription(string.Join("\r\n", ordered
														 .Skip(currentPage * elementsPerPage)
														 .Take(elementsPerPage)
														 .Select(cr => $"**{cr.Key.Trim().ToLowerInvariant()}** `x{cr.Count()}`"))), (int)Math.Ceiling(ordered.Count * 1d / elementsPerPage), reactUsers: new[] { Context.User as IGuildUser })
							 .ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task ShowCustReact(int id) {
			var customReactions = Context.Guild == null
				? Service.GlobalReactions
				: Service.ReactionsForGuild(Context.Guild.Id);

			var found = customReactions.FirstOrDefault(cr => cr?.Id == id);

			if(found == null) {
				await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
				return;
			}
			await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
				.WithDescription($"#{id}")
				.AddField(efb => efb.WithName(GetText("trigger")).WithValue(found.Trigger))
				.AddField(efb => efb.WithName(GetText("response")).WithValue(found.Response + "\n```css\n" + found.Response + "```"))
			).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task DelCustReact(int id) {
			if((Context.Guild == null && !_creds.IsOwner(Context.User)) || (Context.Guild != null && !((IGuildUser)Context.User).GuildPermissions.Administrator)) {
				await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
				return;
			}

			var toDelete = uow.CustomReactions.Get(id);
			if(toDelete != null && ((toDelete.GuildId == null || toDelete.GuildId == 0) && Context.Guild == null || toDelete.GuildId != null && toDelete.GuildId != 0 && Context.Guild.Id == toDelete.GuildId)) {
				uow.CustomReactions.Remove(toDelete);
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
					.WithTitle(GetText("deleted"))
					.WithDescription($"#{toDelete.Id}")
					.AddField(efb => efb.WithName(GetText("trigger")).WithValue(toDelete.Trigger))
					.AddField(efb => efb.WithName(GetText("response")).WithValue(toDelete.Response)));
			} else {
				await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task CrCa(int id) {
			if(Context.Guild == null && !_creds.IsOwner(Context.User) || Context.Guild != null && !((IGuildUser)Context.User).GuildPermissions.Administrator) {
				await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
				return;
			}

			var reactions = Context.Guild == null ? Service.GlobalReactions : Service.ReactionsForGuild(Context.Guild.Id);
			
			if(reactions.Any()) {
				var reaction = reactions.FirstOrDefault(x => x.Id == id);

				if(reaction != null) {
					uow.CustomReactions.Get(id).ContainsAnywhere = !reaction.ContainsAnywhere;
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized(!reaction.ContainsAnywhere ? "crca_enabled" : "crca_disabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
				} else {
					await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
				}
			} else {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task CrDm(int id) {
			if(Context.Guild == null && !_creds.IsOwner(Context.User) || Context.Guild != null && !((IGuildUser)Context.User).GuildPermissions.Administrator) {
				await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
				return;
			}

			var reactions = Context.Guild == null ? Service.GlobalReactions : Service.ReactionsForGuild(Context.Guild.Id);
			
			if(reactions.Any()) {
				var reaction = reactions.FirstOrDefault(x => x.Id == id);

				if(reaction != null) {
					uow.CustomReactions.Get(id).DmResponse = !reaction.DmResponse;
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized(!reaction.DmResponse ? "crdm_enabled" : "crdm_disabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
				} else {
					await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
				}
			} else {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task CrAd(int id) {
			if(Context.Guild == null && !_creds.IsOwner(Context.User) || (Context.Guild != null && !((IGuildUser)Context.User).GuildPermissions.Administrator)) {
				await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
				return;
			}

			var reactions = Context.Guild == null ? Service.GlobalReactions : Service.ReactionsForGuild(Context.Guild.Id);
			
			if(reactions.Any()) {
				var reaction = reactions.FirstOrDefault(x => x.Id == id);

				if(reaction != null) {
					uow.CustomReactions.Get(id).AutoDeleteTrigger = !reaction.AutoDeleteTrigger;
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized(!reaction.AutoDeleteTrigger ? "crad_enabled" : "crad_disabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
				} else {
					await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
				}
			} else {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		public async Task CrStatsClear(string trigger = null) {
			var success = Service.ClearStats(trigger);
			if(string.IsNullOrWhiteSpace(trigger)) {
				await ReplyConfirmLocalized("all_stats_cleared").ConfigureAwait(false);
			} else {
				if(success) {
					await ReplyErrorLocalized("stats_cleared", Format.Bold(trigger)).ConfigureAwait(false);
				} else {
					await ReplyErrorLocalized("stats_not_found").ConfigureAwait(false);
				}
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task CrStats(int page = 1) {
			if(--page < 0)
				return;
			var ordered = Service.ReactionStats.OrderByDescending(x => x.Value).ToArray();
			if(!ordered.Any())
				return;

			const int elementsPerPage = 9;
			await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage => ordered.Skip(currentPage * elementsPerPage).Take(elementsPerPage).Aggregate(new EmbedBuilder().WithOkColor().WithTitle(GetText("stats")), (agg, cur) => agg.AddField(efb => efb.WithName(cur.Key).WithValue(cur.Value.ToString()).WithIsInline(true))), (int)Math.Ceiling(ordered.Length * 1d / elementsPerPage), reactUsers: new[] { Context.User as IGuildUser }, hasPerms: gp => gp.KickMembers).ConfigureAwait(false);
		}
	}
}