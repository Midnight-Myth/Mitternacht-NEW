using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using Mitternacht.Database.Models;
using Mitternacht.Common;
using Mitternacht.Common.Replacements;
using Mitternacht.Database;
using System.Text.RegularExpressions;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class SelfCommands : MitternachtSubmodule<SelfService> {
			private readonly IUnitOfWork uow;

			private static readonly object Locker = new object();
			private readonly DiscordSocketClient _client;
			private readonly IImagesService _images;
			private readonly IBotConfigProvider _bc;

			public SelfCommands(IUnitOfWork uow, DiscordSocketClient client, IImagesService images, IBotConfigProvider bc) {
				this.uow = uow;
				_client = client;
				_images = images;
				_bc = bc;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public async Task StartupCommandAdd([Remainder] string cmdText) {
				var guser = (IGuildUser)Context.User;
				var cmd = new StartupCommand {
					CommandText = cmdText,
					ChannelId = Context.Channel.Id,
					ChannelName = Context.Channel.Name,
					GuildId = Context.Guild?.Id,
					GuildName = Context.Guild?.Name,
					VoiceChannelId = guser.VoiceChannel?.Id,
					VoiceChannelName = guser.VoiceChannel?.Name,
				};

				uow.BotConfig.GetOrCreate().StartupCommands.Add(cmd);
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
					.WithTitle(GetText("scadd"))
					.AddField(efb => efb.WithName(GetText("server"))
						.WithValue(cmd.GuildId == null ? "-" : $"{cmd.GuildName}/{cmd.GuildId}").WithIsInline(true))
					.AddField(efb => efb.WithName(GetText("channel"))
						.WithValue($"{cmd.ChannelName}/{cmd.ChannelId}").WithIsInline(true))
					.AddField(efb => efb.WithName(GetText("command_text"))
						.WithValue(cmdText).WithIsInline(false)));
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public async Task StartupCommands(int page = 1) {
				if(page < 1)
					return;
				page -= 1;
				var scmds = uow.BotConfig
					.GetOrCreate()
					.StartupCommands
					.OrderBy(x => x.Id)
					.ToArray();

				scmds = scmds.Skip(page * 5).Take(5).ToArray();
				if(!scmds.Any()) {
					await ReplyErrorLocalized("startcmdlist_none").ConfigureAwait(false);
				} else {
					await Context.Channel.SendConfirmAsync(string.Join("\n", scmds.Select(x => {
						var str = $"```css\n[{GetText("server") + "]: " + (x.GuildId == null ? "-" : x.GuildName + " #" + x.GuildId)}";

						str += $@"
[{GetText("channel")}]: {x.ChannelName} #{x.ChannelId}
[{GetText("command_text")}]: {x.CommandText}```";
						return str;
					})), "", footer: GetText("page", page + 1))
						 .ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task Wait(int ms) {
				if(ms <= 0)
					return;
				Context.Message.DeleteAfter(0);
				try {
					var msg = await Context.Channel.SendConfirmAsync($"⏲ {ms}ms").ConfigureAwait(false);
					msg.DeleteAfter(ms / 1000);
				} catch { /*ignored*/ }

				await Task.Delay(ms);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public async Task StartupCommandRemove([Remainder] string cmdText) {
				var cmds = uow.BotConfig.GetOrCreate().StartupCommands;
				var cmd = cmds
					.FirstOrDefault(x => x.CommandText.ToLowerInvariant() == cmdText.ToLowerInvariant());

				if(cmd != null) {
					cmds.Remove(cmd);
					await uow.SaveChangesAsync(false).ConfigureAwait(false);
				}

				if(cmd == null)
					await ReplyErrorLocalized("scrm_fail").ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("scrm").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[OwnerOnly]
			public async Task StartupCommandsClear() {
				uow.BotConfig.GetOrCreate().StartupCommands.Clear();
				uow.SaveChanges(false);

				await ReplyConfirmLocalized("startcmds_cleared").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task ForwardMessages() {
				var config = uow.BotConfig.GetOrCreate();
				config.ForwardMessages = !config.ForwardMessages;
				uow.SaveChanges(false);

				_bc.Reload();

				if(Service.ForwardDMs)
					await ReplyConfirmLocalized("fwdm_start").ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("fwdm_stop").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task ForwardToAll() {
				var config = uow.BotConfig.GetOrCreate();
				lock(Locker)
					config.ForwardToAllOwners = !config.ForwardToAllOwners;
				uow.SaveChanges(false);

				_bc.Reload();

				if(Service.ForwardDMsToAllOwners)
					await ReplyConfirmLocalized("fwall_start").ConfigureAwait(false);
				else
					await ReplyConfirmLocalized("fwall_stop").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task Leave([Remainder] string guildStr) {
				guildStr = guildStr.Trim().ToUpperInvariant();
				var server = _client.Guilds.FirstOrDefault(g => g.Id.ToString() == guildStr) ??
					_client.Guilds.FirstOrDefault(g => g.Name.Trim().ToUpperInvariant() == guildStr);

				if(server == null) {
					await ReplyErrorLocalized("no_server").ConfigureAwait(false);
					return;
				}
				if(server.OwnerId != _client.CurrentUser.Id) {
					await server.LeaveAsync().ConfigureAwait(false);
					await ReplyConfirmLocalized("left_server", Format.Bold(server.Name)).ConfigureAwait(false);
				} else {
					await server.DeleteAsync().ConfigureAwait(false);
					await ReplyConfirmLocalized("deleted_server", Format.Bold(server.Name)).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task Die() {
				try { await ReplyConfirmLocalized("shutting_down").ConfigureAwait(false); } catch { /*ignored*/ }

				await Task.Delay(500).ConfigureAwait(false);

				await Task.WhenAny(Task.Run(async () => {
					await _client.StopAsync();
					await _client.LogoutAsync();
				}), Task.Delay(5000));

				Environment.Exit(0);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task SetName([Remainder] string newName) {
				if(string.IsNullOrWhiteSpace(newName))
					return;

				await _client.CurrentUser.ModifyAsync(u => u.Username = newName).ConfigureAwait(false);

				await ReplyConfirmLocalized("bot_name", Format.Bold(newName)).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireUserPermission(GuildPermission.ManageNicknames)]
			[Priority(0)]
			public async Task SetNick([Remainder] string newNick = null) {
				if(string.IsNullOrWhiteSpace(newNick))
					return;
				var curUser = await Context.Guild.GetCurrentUserAsync();
				await curUser.ModifyAsync(u => u.Nickname = newNick).ConfigureAwait(false);

				await ReplyConfirmLocalized("bot_nick", Format.Bold(newNick) ?? "-").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireBotPermission(GuildPermission.ManageNicknames)]
			[RequireUserPermission(GuildPermission.ManageNicknames)]
			[Priority(1)]
			public async Task SetNick(IGuildUser gu, [Remainder] string newNick = null) {
				await gu.ModifyAsync(u => u.Nickname = newNick).ConfigureAwait(false);

				await ReplyConfirmLocalized("user_nick", Format.Bold(gu.ToString()), Format.Bold(newNick) ?? "-").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task SetStatus([Remainder] SettableUserStatus status) {
				await _client.SetStatusAsync(SettableUserStatusToUserStatus(status)).ConfigureAwait(false);

				await ReplyConfirmLocalized("bot_status", Format.Bold(status.ToString())).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task SetAvatar([Remainder] string img = null) {
				if(string.IsNullOrWhiteSpace(img))
					return;

				using var http = new HttpClient();
				using var sr = await http.GetStreamAsync(img);
				var imgStream = new MemoryStream();
				await sr.CopyToAsync(imgStream);
				imgStream.Position = 0;

				await _client.CurrentUser.ModifyAsync(u => u.Avatar = new Image(imgStream)).ConfigureAwait(false);

				await ReplyConfirmLocalized("set_avatar").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task SetActivity(ActivityType type, [Remainder] string activityName = "") {
				await _client.SetGameAsync(activityName, type: type).ConfigureAwait(false);

				await ReplyConfirmLocalized("set_activity").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task SetStream(string url, [Remainder] string name = "") {
				await _client.SetGameAsync(name, url, ActivityType.Streaming).ConfigureAwait(false);

				await ReplyConfirmLocalized("set_stream").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task Send(string where, [Remainder] string msg = "") {
				if(string.IsNullOrWhiteSpace(msg))
					return;

				var destinationMatch = Regex.Match(where, "\\A(?<serverId>\\d+)\\|(?<channelOrUser>c|u):(?<channelOrUserId>\\d+)\\z", RegexOptions.IgnoreCase);

				if(destinationMatch.Success && ulong.TryParse(destinationMatch.Groups["serverId"].Value, out var destinationServerId) && ulong.TryParse(destinationMatch.Groups["channelOrUserId"].Value, out var destinationChannelOrUserId)) {
					var channelOrUser = destinationMatch.Groups["channelOrUser"].Value;

					var guild = _client.Guilds.FirstOrDefault(g => g.Id == destinationServerId);

					if(guild != null) {
						var rep = new ReplacementBuilder().WithDefault(Context).Build();

						if(channelOrUser.Equals("c", StringComparison.OrdinalIgnoreCase)) {
							var channel = guild.TextChannels.FirstOrDefault(c => c.Id == destinationChannelOrUserId);

							if(channel != null) {
								if(CREmbed.TryParse(msg, out var crembed)) {
									rep.Replace(crembed);
									await channel.EmbedAsync(crembed.ToEmbedBuilder(), crembed.PlainText?.SanitizeMentions() ?? "").ConfigureAwait(false);
								} else {
									await channel.SendMessageAsync($"{rep.Replace(msg)?.SanitizeMentions() ?? ""}");
								}
								
								await ReplyConfirmLocalized("message_sent").ConfigureAwait(false);
							}
						} else if(channelOrUser.Equals("u", StringComparison.OrdinalIgnoreCase)) {
							var user = guild.Users.FirstOrDefault(u => u.Id == destinationChannelOrUserId);
							
							if(user != null) {
								if(CREmbed.TryParse(msg, out var crembed)) {
									rep.Replace(crembed);
									await (await user.CreateDMChannelAsync()).EmbedAsync(crembed.ToEmbedBuilder(), crembed.PlainText?.SanitizeMentions() ?? "").ConfigureAwait(false);
								} else {
									await (await user.CreateDMChannelAsync()).SendMessageAsync($"`#{msg}` {rep.Replace(msg)?.SanitizeMentions() ?? ""}");
								}
								
								await ReplyConfirmLocalized("message_sent").ConfigureAwait(false);
							}
						}
					}
				} else {
					await ReplyErrorLocalized("invalid_format").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task Announce([Remainder] string message) {
				var channels = _client.Guilds.Select(g => g.DefaultChannel).ToArray();
				await Task.WhenAll(channels.Where(c => c != null).Select(c => c.SendConfirmAsync(message, GetText("message_from_bo", Context.User.ToString())))).ConfigureAwait(false);
				await ReplyConfirmLocalized("message_sent").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task ReloadImages() {
				var sw = Stopwatch.StartNew();
				_images.Reload();
				sw.Stop();
				await ReplyConfirmLocalized("images_loaded", sw.Elapsed.TotalSeconds.ToString("F3")).ConfigureAwait(false);
			}

			private static UserStatus SettableUserStatusToUserStatus(SettableUserStatus sus) {
				return sus switch {
					SettableUserStatus.Online => UserStatus.Online,
					SettableUserStatus.Invisible => UserStatus.Invisible,
					SettableUserStatus.Idle => UserStatus.AFK,
					SettableUserStatus.Dnd => UserStatus.DoNotDisturb,
					_ => UserStatus.Online,
				};
			}

			public enum SettableUserStatus {
				Online,
				Invisible,
				Idle,
				Dnd
			}
		}
	}
}
