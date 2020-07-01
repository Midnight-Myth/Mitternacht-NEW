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
using Mitternacht.Services.Database.Models;
using Newtonsoft.Json;

namespace Mitternacht.Modules.CustomReactions {
	public class CustomReactions : MitternachtTopLevelModule<CustomReactionsService> {
		private readonly IBotCredentials _creds;
		private readonly DbService _db;
		private readonly DiscordSocketClient _client;

		public CustomReactions(IBotCredentials creds, DbService db,
			DiscordSocketClient client) {
			_creds = creds;
			_db = db;
			_client = client;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task AddCustReact(string key, [Remainder] string message) {
			var channel = Context.Channel as ITextChannel;
			if(string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
				return;

			key = key.ToLowerInvariant();

			if((channel == null && !_creds.IsOwner(Context.User)) || (channel != null && !((IGuildUser)Context.User).GuildPermissions.Administrator)) {
				await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
				return;
			}

			var cr = new CustomReaction()
			{
				GuildId = channel?.Guild.Id,
				IsRegex = false,
				Trigger = key,
				Response = message,
			};

			using(var uow = _db.UnitOfWork) {
				uow.CustomReactions.Add(cr);

				await uow.CompleteAsync().ConfigureAwait(false);
			}

			if(channel == null) {
				Array.Resize(ref Service.GlobalReactions, Service.GlobalReactions.Length + 1);
				Service.GlobalReactions[Service.GlobalReactions.Length - 1] = cr;
			} else {
				Service.GuildReactions.AddOrUpdate(Context.Guild.Id,
					new[] { cr },
					(k, old) => {
						Array.Resize(ref old, old.Length + 1);
						old[old.Length - 1] = cr;
						return old;
					});
			}

			await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
				.WithTitle(GetText("new_cust_react"))
				.WithDescription($"#{cr.Id}")
				.AddField(efb => efb.WithName(GetText("trigger")).WithValue(key))
				.AddField(efb => efb.WithName(GetText("response")).WithValue(message.Length > 1024 ? GetText("redacted_too_long") : message))
				).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[Priority(1)]
		public async Task ListCustReact(int page = 1) {
			if(--page < 0 || page > 999)
				return;
			var customReactions = Context.Guild == null
				? Service.GlobalReactions.Where(cr => cr != null).ToArray()
				: Service.GuildReactions.GetOrAdd(Context.Guild.Id, Array.Empty<CustomReaction>())
					.Where(cr => cr != null).ToArray();

			if(!customReactions.Any()) {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
				return;
			}

			var lastPage = customReactions.Length / 20;
			await Context.Channel.SendPaginatedConfirmAsync(_client, page,
				curPage => new EmbedBuilder().WithOkColor().WithTitle(GetText("name")).WithDescription(string.Join("\n",
					customReactions.OrderBy(cr => cr.Trigger).Skip(curPage * 20).Take(20).Select(cr => {
						var str = $"`#{cr.Id}` {cr.Trigger}";
						if(cr.AutoDeleteTrigger) {
							str = "🗑" + str;
						}
						if(cr.DmResponse) {
							str = "📪" + str;
						}
						return str;
					}))), lastPage, reactUsers: new[] { Context.User as IGuildUser }).ConfigureAwait(false);
		}

		public enum All {
			All
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[Priority(0)]
		public async Task ListCustReact(All x) {
			var customReactions = Context.Guild == null
				? Service.GlobalReactions.Where(cr => cr != null).ToArray()
				: Service.GuildReactions.GetOrAdd(Context.Guild.Id, new CustomReaction[] { }).Where(cr => cr != null)
					.ToArray();

			if(!customReactions.Any()) {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
				return;
			}

			var txtStream = await JsonConvert.SerializeObject(customReactions.GroupBy(cr => cr.Trigger)
														.OrderBy(cr => cr.Key)
														.Select(cr => new { Trigger = cr.Key, Responses = cr.Select(y => new { id = y.Id, text = y.Response }).ToList() }), Formatting.Indented)
														.ToStream()
														.ConfigureAwait(false);

			if(Context.Guild == null) // its a private one, just send back
				await Context.Channel.SendFileAsync(txtStream, "customreactions.txt", GetText("list_all")).ConfigureAwait(false);
			else
				await ((IGuildUser)Context.User).SendFileAsync(txtStream, "customreactions.txt", GetText("list_all")).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task ListCustReactG(int page = 1) {
			if(--page < 0 || page > 9999)
				return;
			var customReactions = Context.Guild == null
				? Service.GlobalReactions.Where(cr => cr != null).ToArray()
				: Service.GuildReactions.GetOrAdd(Context.Guild.Id, new CustomReaction[] { }).Where(cr => cr != null)
					.ToArray();

			if(!customReactions.Any()) {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
			} else {
				var ordered = customReactions
					.GroupBy(cr => cr.Trigger)
					.OrderBy(cr => cr.Key)
					.ToList();

				var lastPage = ordered.Count / 20;
				await Context.Channel.SendPaginatedConfirmAsync(_client, page, curPage =>
					new EmbedBuilder().WithOkColor()
						.WithTitle(GetText("name"))
						.WithDescription(string.Join("\r\n", ordered
														 .Skip(curPage * 20)
														 .Take(20)
														 .Select(cr => $"**{cr.Key.Trim().ToLowerInvariant()}** `x{cr.Count()}`"))), lastPage, reactUsers: new[] { Context.User as IGuildUser })
							 .ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task ShowCustReact(int id) {
			var customReactions = Context.Guild == null
				? Service.GlobalReactions
				: Service.GuildReactions.GetOrAdd(Context.Guild.Id, new CustomReaction[] { });

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

			var success = false;
			CustomReaction toDelete;
			using(var uow = _db.UnitOfWork) {
				toDelete = uow.CustomReactions.Get(id);
				if(toDelete != null) {
					if((toDelete.GuildId == null || toDelete.GuildId == 0) && Context.Guild == null) {
						uow.CustomReactions.Remove(toDelete);
						//todo 91 i can dramatically improve performance of this, if Ids are ordered.
						Service.GlobalReactions = Service.GlobalReactions.Where(cr => cr?.Id != toDelete.Id)
							.ToArray();
						success = true;
					} else if((toDelete.GuildId != null && toDelete.GuildId != 0) &&
							   Context.Guild.Id == toDelete.GuildId) {
						uow.CustomReactions.Remove(toDelete);
						Service.GuildReactions.AddOrUpdate(Context.Guild.Id, new CustomReaction[] { },
							(key, old) => old.Where(cr => cr?.Id != toDelete.Id).ToArray());
						success = true;
					}
					if(success)
						await uow.CompleteAsync().ConfigureAwait(false);
				}
			}

			if(success) {
				await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
					.WithTitle(GetText("deleted"))
					.WithDescription("#" + toDelete.Id)
					.AddField(efb => efb.WithName(GetText("trigger")).WithValue(toDelete.Trigger))
					.AddField(efb => efb.WithName(GetText("response")).WithValue(toDelete.Response)));
			} else {
				await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task CrCa(int id) {
			if((Context.Guild == null && !_creds.IsOwner(Context.User)) ||
				(Context.Guild != null && !((IGuildUser)Context.User).GuildPermissions.Administrator)) {
				await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
				return;
			}

			CustomReaction[] reactions;

			if(Context.Guild == null)
				reactions = Service.GlobalReactions;
			else {
				Service.GuildReactions.TryGetValue(Context.Guild.Id, out reactions);
			}
			if(reactions.Any()) {
				var reaction = reactions.FirstOrDefault(x => x.Id == id);

				if(reaction == null) {
					await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
					return;
				}

				var setValue = reaction.ContainsAnywhere = !reaction.ContainsAnywhere;

				using(var uow = _db.UnitOfWork) {
					uow.CustomReactions.Get(id).ContainsAnywhere = setValue;
					uow.Complete();
				}

				if(setValue) {
					await ReplyConfirmLocalized("crca_enabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
				} else {
					await ReplyConfirmLocalized("crca_disabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
				}
			} else {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task CrDm(int id) {
			if((Context.Guild == null && !_creds.IsOwner(Context.User)) ||
				(Context.Guild != null && !((IGuildUser)Context.User).GuildPermissions.Administrator)) {
				await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
				return;
			}

			CustomReaction[] reactions;

			if(Context.Guild == null)
				reactions = Service.GlobalReactions;
			else {
				Service.GuildReactions.TryGetValue(Context.Guild.Id, out reactions);
			}
			if(reactions.Any()) {
				var reaction = reactions.FirstOrDefault(x => x.Id == id);

				if(reaction == null) {
					await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
					return;
				}

				var setValue = reaction.DmResponse = !reaction.DmResponse;

				using(var uow = _db.UnitOfWork) {
					uow.CustomReactions.Get(id).DmResponse = setValue;
					uow.Complete();
				}

				if(setValue) {
					await ReplyConfirmLocalized("crdm_enabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
				} else {
					await ReplyConfirmLocalized("crdm_disabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
				}
			} else {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task CrAd(int id) {
			if((Context.Guild == null && !_creds.IsOwner(Context.User)) ||
				(Context.Guild != null && !((IGuildUser)Context.User).GuildPermissions.Administrator)) {
				await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
				return;
			}

			CustomReaction[] reactions;

			if(Context.Guild == null)
				reactions = Service.GlobalReactions;
			else {
				Service.GuildReactions.TryGetValue(Context.Guild.Id, out reactions);
			}
			if(reactions.Any()) {
				var reaction = reactions.FirstOrDefault(x => x.Id == id);

				if(reaction == null) {
					await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
					return;
				}

				var setValue = reaction.AutoDeleteTrigger = !reaction.AutoDeleteTrigger;

				using(var uow = _db.UnitOfWork) {
					uow.CustomReactions.Get(id).AutoDeleteTrigger = setValue;
					uow.Complete();
				}

				if(setValue) {
					await ReplyConfirmLocalized("crad_enabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
				} else {
					await ReplyConfirmLocalized("crad_disabled", Format.Code(reaction.Id.ToString())).ConfigureAwait(false);
				}
			} else {
				await ReplyErrorLocalized("no_found").ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		public async Task CrStatsClear(string trigger = null) {
			if(string.IsNullOrWhiteSpace(trigger)) {
				Service.ClearStats();
				await ReplyConfirmLocalized("all_stats_cleared").ConfigureAwait(false);
			} else {
				if(Service.ReactionStats.TryRemove(trigger, out _)) {
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
			var lastPage = ordered.Length / 9;
			await Context.Channel.SendPaginatedConfirmAsync(_client, page,
				curPage => ordered.Skip(curPage * 9).Take(9)
					.Aggregate(new EmbedBuilder().WithOkColor().WithTitle(GetText("stats")),
						(agg, cur) => agg.AddField(efb => efb.WithName(cur.Key).WithValue(cur.Value.ToString())
							.WithIsInline(true))), lastPage, reactUsers: new[] { Context.User as IGuildUser }, hasPerms: gp => gp.KickMembers).ConfigureAwait(false);
		}
	}
}