using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Administration {
	public partial class Administration {
		[Group]
		public class ProtectionCommands : MitternachtSubmodule<ProtectionService> {
			private readonly MuteService _mute;
			private readonly IUnitOfWork uow;

			public ProtectionCommands(MuteService mute, IUnitOfWork uow) {
				_mute = mute;
				this.uow = uow;
			}

			private string GetAntiSpamString(AntiSpamSetting antiSpamSettings) {
				var ignoredString = string.Join(", ", antiSpamSettings.IgnoredChannels.Select(c => $"<#{c.ChannelId}>"));

				if(string.IsNullOrWhiteSpace(ignoredString))
					ignoredString = "none";

				var add = antiSpamSettings.Action == PunishmentAction.Mute && antiSpamSettings.MuteTime > 0 ? $" ({antiSpamSettings.MuteTime}s)" : "";

				return GetText("spam_stats", Format.Bold(antiSpamSettings.MessageThreshold.ToString()), Format.Bold($"{antiSpamSettings.Action}{add}"), ignoredString);
			}

			private string GetAntiRaidString(AntiRaidSetting antiRaidSettings)
				=> GetText("raid_stats", Format.Bold(antiRaidSettings.UserThreshold.ToString()), Format.Bold(antiRaidSettings.Seconds.ToString()), Format.Bold(antiRaidSettings.Action.ToString()));

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task AntiRaid(int userThreshold = 5, int seconds = 10, PunishmentAction action = PunishmentAction.Mute) {
				if(userThreshold < 2 || userThreshold > 30) {
					await ReplyErrorLocalized("raid_cnt", 2, 30).ConfigureAwait(false);
					return;
				}

				if(seconds < 2 || seconds > 300) {
					await ReplyErrorLocalized("raid_time", 2, 300).ConfigureAwait(false);
					return;
				}

				var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiRaidSetting));

				if(gc.AntiRaidSetting != null) {
					gc.AntiRaidSetting = null;
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized("prot_disable", "Anti-Raid").ConfigureAwait(false);
				} else {
					try {
						await _mute.GetMuteRole(Context.Guild).ConfigureAwait(false);
					} catch(Exception ex) {
						_log.Warn(ex);
						await ReplyErrorLocalized("prot_error").ConfigureAwait(false);
						return;
					}

					gc.AntiRaidSetting = new AntiRaidSetting {
						Action = action,
						Seconds = seconds,
						UserThreshold = userThreshold,
					};
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await Context.Channel.SendConfirmAsync($"{Context.User.Mention} {GetAntiRaidString(gc.AntiRaidSetting)}", GetText("prot_enable", "Anti-Raid")).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			[Priority(1)]
			public async Task AntiSpam() {
				var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting).ThenInclude(x => x.IgnoredChannels));

				if(gc.AntiSpamSetting != null) {
					Service.ResetSpamForGuild(Context.Guild.Id);

					gc.AntiSpamSetting = null;
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized("prot_disable", "Anti-Spam").ConfigureAwait(false);
					return;
				} else {
					await AntiSpam(3).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			[Priority(0)]
			public async Task AntiSpam(int messageCount, PunishmentAction action = PunishmentAction.Mute, int time = 0) {
				if(messageCount < 2 || messageCount > 10)
					return;

				if(time < 0 || time > 60 * 12)
					return;

				try {
					await _mute.GetMuteRole(Context.Guild).ConfigureAwait(false);
				} catch(Exception ex) {
					_log.Warn(ex);
					await ReplyErrorLocalized("prot_error").ConfigureAwait(false);
					return;
				}

				var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting));

				gc.AntiSpamSetting = new AntiSpamSetting {
					Action = action,
					MessageThreshold = messageCount,
					MuteTime = time,
				};
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await Context.Channel.SendConfirmAsync($"{Context.User.Mention} {GetAntiSpamString(gc.AntiSpamSetting)}", GetText("prot_enable", "Anti-Spam")).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task AntispamIgnore() {
				var antiSpamSetting = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiSpamSetting).ThenInclude(x => x.IgnoredChannels)).AntiSpamSetting;
				
				if(antiSpamSetting == null) {
					return;
				}

				if(antiSpamSetting.IgnoredChannels.Any(c => c.ChannelId == Context.Channel.Id)) {
					antiSpamSetting.IgnoredChannels.Add(new AntiSpamIgnore {
						ChannelId = Context.Channel.Id
					});

					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized("spam_ignore", "Anti-Spam").ConfigureAwait(false);
				} else {
					antiSpamSetting.IgnoredChannels.RemoveWhere(x => x.ChannelId == Context.Channel.Id);
					await uow.SaveChangesAsync(false).ConfigureAwait(false);

					await ReplyConfirmLocalized("spam_not_ignore", "Anti-Spam").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task AntiList() {
				var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(x => x.AntiRaidSetting).Include(x => x.AntiSpamSetting));
				var antiSpamSetting = gc.AntiSpamSetting;
				var antiRaidSetting = gc.AntiRaidSetting;

				if(antiSpamSetting == null && antiRaidSetting == null) {
					await ReplyConfirmLocalized("prot_none").ConfigureAwait(false);
					return;
				}

				var embed = new EmbedBuilder().WithOkColor().WithTitle(GetText("prot_active"));

				if(antiSpamSetting != null)
					embed.AddField(efb => efb.WithName("Anti-Spam")
						.WithValue(GetAntiSpamString(antiSpamSetting))
						.WithIsInline(true));

				if(antiRaidSetting != null)
					embed.AddField(efb => efb.WithName("Anti-Raid")
						.WithValue(GetAntiRaidString(antiRaidSetting))
						.WithIsInline(true));

				await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
			}
		}
	}
}
