using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;
using System.Linq.Expressions;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class GuildConfigRepository : Repository<GuildConfig>, IGuildConfigRepository {
		public GuildConfigRepository(DbContext context) : base(context) { }

		private static List<WarningPunishment> DefaultWarnPunishments
			=> new List<WarningPunishment> {
				new WarningPunishment {
					Count = 3,
					Punishment = PunishmentAction.Kick
				},
				new WarningPunishment {
					Count = 5,
					Punishment = PunishmentAction.Ban
				}
			};

		public IEnumerable<GuildConfig> GetAllGuildConfigs(List<ulong> availableGuilds) {
			var guildConfigs = _set.Where((Expression<Func<GuildConfig, bool>>)(gc => availableGuilds.Contains(gc.GuildId))).ToList();
			foreach(var guildConfig in guildConfigs) {
				_context.Entry(guildConfig).Reference(gc => gc.LogSetting).Load();
				if(guildConfig.LogSetting != null)
					_context.Entry(guildConfig.LogSetting).Collection(ls => ls.IgnoredChannels).Load();
				_context.Entry(guildConfig).Collection(gc => gc.MutedUsers).Load();
				_context.Entry(guildConfig).Collection(gc => gc.CommandAliases).Load();
				_context.Entry(guildConfig).Collection(gc => gc.UnmuteTimers).Load();
				_context.Entry(guildConfig).Collection(gc => gc.VcRoleInfos).Load();
				_context.Entry(guildConfig).Collection(gc => gc.GenerateCurrencyChannelIds).Load();
				_context.Entry(guildConfig).Collection(gc => gc.FilterInvitesChannelIds).Load();
				_context.Entry(guildConfig).Collection(gc => gc.FilterWordsChannelIds).Load();
				_context.Entry(guildConfig).Collection(gc => gc.FilteredWords).Load();
				_context.Entry(guildConfig).Collection(gc => gc.CommandCooldowns).Load();
				_context.Entry(guildConfig).Collection(gc => gc.GuildRepeaters).Load();
				_context.Entry(guildConfig).Reference(gc => gc.AntiRaidSetting).Load();
				_context.Entry(guildConfig).Collection(gc => gc.SlowmodeIgnoredRoles).Load();
				_context.Entry(guildConfig).Collection(gc => gc.SlowmodeIgnoredUsers).Load();
				_context.Entry(guildConfig).Reference(gc => gc.AntiSpamSetting).Load();
				if(guildConfig.AntiSpamSetting != null)
					_context.Entry(guildConfig.AntiSpamSetting).Collection(x => x.IgnoredChannels).Load();
				_context.Entry(guildConfig).Collection(gc => gc.FollowedStreams).Load();
				_context.Entry(guildConfig).Reference(gc => gc.StreamRole).Load();
				_context.Entry(guildConfig).Collection(gc => gc.NsfwBlacklistedTags).Load();
			}
			return guildConfigs;
		}

		/// <summary>
		/// Gets and creates if it doesn't exist a config for a guild.
		/// </summary>
		/// <param name="guildId">For which guild</param>
		/// <param name="includes">Use to manipulate the set however you want</param>
		/// <returns>Config for the guild</returns>
		public GuildConfig For(ulong guildId, Func<DbSet<GuildConfig>, IQueryable<GuildConfig>> includes = null) {
			GuildConfig config;

			if(includes == null) {
				config = _set
					.Include(gc => gc.FollowedStreams)
					.Include(gc => gc.LogSetting)
						.ThenInclude(ls => ls.IgnoredChannels)
					.Include(gc => gc.FilterInvitesChannelIds)
					.Include(gc => gc.FilterWordsChannelIds)
					.Include(gc => gc.FilteredWords)
					.Include(gc => gc.FilterZalgoChannelIds)
					.Include(gc => gc.GenerateCurrencyChannelIds)
					.Include(gc => gc.CommandCooldowns)
					.FirstOrDefault(c => c.GuildId == guildId);
			} else {
				var set = includes(_set);
				config = set.FirstOrDefault(c => c.GuildId == guildId);
			}

			if(config == null) {
				_set.Add(config = new GuildConfig {
					GuildId = guildId,
					Permissions = Permissionv2.GetDefaultPermlist,
					WarningsInitialized = true,
					WarnPunishments = DefaultWarnPunishments,
				});
				_context.SaveChanges();
			}

			if(!config.WarningsInitialized) {
				config.WarningsInitialized = true;
				config.WarnPunishments = DefaultWarnPunishments;
			}

			return config;
		}

		public GuildConfig LogSettingsFor(ulong guildId) {
			var config = _set.Include(gc => gc.LogSetting)
							.ThenInclude(gc => gc.IgnoredChannels)
			   .FirstOrDefault(x => x.GuildId == guildId);

			if(config == null) {
				_set.Add(config = new GuildConfig {
					GuildId = guildId,
					Permissions = Permissionv2.GetDefaultPermlist,
					WarningsInitialized = true,
					WarnPunishments = DefaultWarnPunishments,
				});
				_context.SaveChanges();
			}

			if(config.WarningsInitialized)
				return config;
			config.WarningsInitialized = true;
			config.WarnPunishments = DefaultWarnPunishments;
			return config;
		}

		public IEnumerable<GuildConfig> OldPermissionsForAll() {
			var query = _set
				.Where((Expression<Func<GuildConfig, bool>>)(gc => gc.RootPermission != null))
				.Include(gc => gc.RootPermission);

			for(var i = 0; i < 60; i++) {
				query = query.ThenInclude(gc => gc.Next);
			}

			return query.ToList();
		}

		public IEnumerable<GuildConfig> Permissionsv2ForAll(List<ulong> include) {
			var query = _set.Where((Expression<Func<GuildConfig, bool>>)(x => include.Contains(x.GuildId))).Include(gc => gc.Permissions);

			return query.ToList();
		}

		public GuildConfig GcWithPermissionsv2For(ulong guildId) {
			var config = _set.Where((Expression<Func<GuildConfig, bool>>)(gc => gc.GuildId == guildId)).Include(gc => gc.Permissions).FirstOrDefault();

			if(config == null) {
				_set.Add(config = new GuildConfig {
					GuildId = guildId,
					Permissions = Permissionv2.GetDefaultPermlist
				});
				_context.SaveChanges();
			} else if(config.Permissions == null || !config.Permissions.Any()) {
				config.Permissions = Permissionv2.GetDefaultPermlist;
				_context.SaveChanges();
			}

			return config;
		}

		public IEnumerable<FollowedStream> GetAllFollowedStreams(List<ulong> included)
			=> _set.Where((Expression<Func<GuildConfig, bool>>)(gc => included.Contains(gc.GuildId)))
				.Include(gc => gc.FollowedStreams)
				.ToList()
				.SelectMany(gc => gc.FollowedStreams)
				.ToList();

		public void SetCleverbotEnabled(ulong id, bool cleverbotEnabled) {
			var conf = _set.FirstOrDefault(gc => gc.GuildId == id);

			if(conf != null)
				conf.CleverbotEnabled = cleverbotEnabled;
		}
	}
}
