using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.Collections;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Gambling {
	public partial class Gambling {
		[Group]
		public class FlowerShopCommands : MitternachtSubmodule {
			private readonly IBotConfigProvider _bc;
			private readonly IUnitOfWork uow;
			private readonly CurrencyService _cs;

			public enum Role {
				Role
			}

			public enum List {
				List
			}

			public FlowerShopCommands(IBotConfigProvider bc, IUnitOfWork uow, CurrencyService cs) {
				this.uow     = uow;
				_bc     = bc;
				_cs     = cs;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Shop(int page = 1) {
				if(--page < 0)
					return;

				var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.ShopEntries).ThenInclude(x => x.Items)).ShopEntries);

				const int elementsPerPage = 9;
				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage => {
					var theseEntries = entries.Skip(currentPage * elementsPerPage).Take(elementsPerPage).ToArray();

					if(!theseEntries.Any())
						return new EmbedBuilder().WithErrorColor()
							.WithDescription(GetText("shop_none"));
					var embed = new EmbedBuilder().WithOkColor()
						.WithTitle(GetText("shop", _bc.BotConfig.CurrencySign));

					for(var i = 0; i < theseEntries.Length; i++) {
						var entry = theseEntries[i];
						embed.AddField(efb => efb.WithName($"#{currentPage * elementsPerPage + i + 1} - {entry.Price}{_bc.BotConfig.CurrencySign}").WithValue(EntryToString(entry)).WithIsInline(true));
					}
					return embed;
				}, (int)Math.Ceiling(entries.Count * 1d / elementsPerPage), reactUsers: new[] { Context.User as IGuildUser });
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task Buy(int index, [Remainder] string message = null) {
				index -= 1;
				if(index < 0)
					return;
				var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.ShopEntries).ThenInclude(x => x.Items));
				var entries = new IndexedCollection<ShopEntry>(gc.ShopEntries);
				var entry = entries.ElementAtOrDefault(index);

				if(entry == null) {
					await ReplyErrorLocalized("shop_item_not_found").ConfigureAwait(false);
					return;
				}
				
				var guildUser = (IGuildUser)Context.User;

				if(entry.Type == ShopEntryType.Role) {
					var role = Context.Guild.GetRole(entry.RoleId);

					if(role == null) {
						await ReplyErrorLocalized("shop_role_not_found").ConfigureAwait(false);
						return;
					}

					if(await _cs.RemoveAsync(guildUser, $"Shop purchase - {entry.Type}", entry.Price).ConfigureAwait(false)) {
						try {
							await guildUser.AddRoleAsync(role).ConfigureAwait(false);
						} catch(Exception ex) {
							_log.Warn(ex);
							await _cs.AddAsync(guildUser, "Shop error refund", entry.Price);
							await ReplyErrorLocalized("shop_role_purchase_error").ConfigureAwait(false);
							return;
						}
						await _cs.AddAsync(Context.Guild.Id, entry.AuthorId, $"Shop sell item - {entry.Type}", GetProfitAmount(entry.Price), uow).ConfigureAwait(false);

						await ReplyConfirmLocalized("shop_role_purchase", Format.Bold(role.Name)).ConfigureAwait(false);
					} else {
						await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
					}
				} else if(entry.Type == ShopEntryType.List) {
					if(entry.Items.Count == 0) {
						await ReplyErrorLocalized("out_of_stock").ConfigureAwait(false);
						return;
					}

					var item = entry.Items.ToArray()[new NadekoRandom().Next(0, entry.Items.Count)];

					if(await _cs.RemoveAsync(guildUser, $"Shop purchase - {entry.Type}", entry.Price)) {
						uow.Context.Set<ShopEntryItem>().Remove(item);
						await uow.SaveChangesAsync(false).ConfigureAwait(false);
						try {
							await (await Context.User.GetOrCreateDMChannelAsync())
								.EmbedAsync(new EmbedBuilder().WithOkColor()
								.WithTitle(GetText("shop_purchase", Context.Guild.Name))
								.AddField(efb => efb.WithName(GetText("item")).WithValue(item.Text).WithIsInline(false))
								.AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
								.AddField(efb => efb.WithName(GetText("name")).WithValue(entry.Name).WithIsInline(true)))
								.ConfigureAwait(false);

							await _cs.AddAsync(Context.Guild.Id, entry.AuthorId, $"Shop sell item - {entry.Name}", GetProfitAmount(entry.Price)).ConfigureAwait(false);
						} catch {
							uow.Context.Set<ShopEntryItem>().Add(item);
							await uow.SaveChangesAsync(false).ConfigureAwait(false);

							await _cs.AddAsync(guildUser, $"Shop error refund - {entry.Name}", entry.Price).ConfigureAwait(false);
							await ReplyErrorLocalized("shop_buy_error").ConfigureAwait(false);
							return;
						}
						await ReplyConfirmLocalized("shop_item_purchase").ConfigureAwait(false);
					} else {
						await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
					}
				}
			}

			private long GetProfitAmount(int price)
				=> (int)Math.Ceiling(0.90 * price);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			[RequireBotPermission(GuildPermission.ManageRoles)]
			public async Task ShopAdd(Role _, int price, [Remainder] IRole role) {
				var entry = new ShopEntry {
					Name     = "-",
					Price    = price,
					Type     = ShopEntryType.Role,
					AuthorId = Context.User.Id,
					RoleId   = role.Id,
					RoleName = role.Name
				};

				var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.ShopEntries).ThenInclude(x => x.Items)).ShopEntries) {
					entry
				};
				uow.GuildConfigs.For(Context.Guild.Id).ShopEntries = entries;
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await Context.Channel.EmbedAsync(EntryToEmbed(entry).WithTitle(GetText("shop_item_add")));
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task ShopAdd(List _, int price, [Remainder] string name) {
				var entry = new ShopEntry {
					Name     = name.TrimTo(100),
					Price    = price,
					Type     = ShopEntryType.List,
					AuthorId = Context.User.Id,
					Items    = new HashSet<ShopEntryItem>(),
				};

				var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.ShopEntries).ThenInclude(x => x.Items)).ShopEntries) {
					entry
				};
				uow.GuildConfigs.For(Context.Guild.Id).ShopEntries = entries;
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await Context.Channel.EmbedAsync(EntryToEmbed(entry).WithTitle(GetText("shop_item_add")));
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task ShopListAdd(int index, [Remainder] string itemText) {
				index -= 1;
				if(index < 0)
					return;

				var item = new ShopEntryItem {
					Text = itemText
				};
				var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.ShopEntries).ThenInclude(x => x.Items)).ShopEntries);
				var entry   = entries.ElementAtOrDefault(index);

				if(entry != null) {
					if(entry.Type == ShopEntryType.List) {
						if(entry.Items.Add(item)) {
							await uow.SaveChangesAsync(false).ConfigureAwait(false);

							await ReplyConfirmLocalized("shop_list_item_added").ConfigureAwait(false);
						} else {
							await ReplyErrorLocalized("shop_list_item_not_unique").ConfigureAwait(false);
						}
					} else {
						await ReplyErrorLocalized("shop_item_wrong_type").ConfigureAwait(false);
					}
				} else {
					await ReplyErrorLocalized("shop_item_not_found").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task ShopRemove(int index) {
				index -= 1;
				if(index < 0)
					return;

				var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.ShopEntries).ThenInclude(x => x.Items));

				var entries = new IndexedCollection<ShopEntry>(gc.ShopEntries);
				var entryToRemove = entries.ElementAtOrDefault(index);
				if(entryToRemove != null) {
					entries.Remove(entryToRemove);

					gc.ShopEntries = entries;
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await Context.Channel.EmbedAsync(EntryToEmbed(entryToRemove).WithTitle(GetText("shop_item_rm")));
				} else {
					await ReplyErrorLocalized("shop_item_not_found").ConfigureAwait(false);
				}
			}

			public EmbedBuilder EntryToEmbed(ShopEntry entry) {
				var eb = new EmbedBuilder().WithOkColor();

				switch(entry.Type) {
					case ShopEntryType.Role:
						return eb.AddField(efb => efb.WithName(GetText("name")).WithValue(GetText("shop_role", Format.Bold(entry.RoleName))).WithIsInline(true))
							.AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
							.AddField(efb => efb.WithName(GetText("type")).WithValue(entry.Type.ToString()).WithIsInline(true));
					case ShopEntryType.List:
						return eb.AddField(efb => efb.WithName(GetText("name")).WithValue(entry.Name).WithIsInline(true))
							.AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
							.AddField(efb => efb.WithName(GetText("type")).WithValue(GetText("random_unique_item")).WithIsInline(true));
					default:
						return null;
				}
			}

			public string EntryToString(ShopEntry entry)
				=> entry.Type switch {
					ShopEntryType.Role => GetText("shop_role", Format.Bold(entry.RoleName)),
					ShopEntryType.List => $"{GetText("unique_items_left", entry.Items.Count)}\n{entry.Name}",
					_                  => "",
				};
		}
	}
}