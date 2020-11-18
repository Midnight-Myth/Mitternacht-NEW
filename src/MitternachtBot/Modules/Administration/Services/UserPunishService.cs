using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services;
using Mitternacht.Database.Models;
using Mitternacht.Services.Impl;
using Mitternacht.Common;

namespace Mitternacht.Modules.Administration.Services {
	public class UserPunishService : IMService {
		private readonly MuteService _mute;
		private readonly DbService   _db;

		public UserPunishService(MuteService mute, DbService db) {
			_mute = mute;
			_db   = db;
		}

		public async Task<PunishmentAction?> Warn(IGuild guild, ulong userId, string modName, string reason) {
			if(string.IsNullOrWhiteSpace(reason))
				reason = "-";

			var guildId = guild.Id;

			var warn = new Warning {
				UserId    = userId,
				GuildId   = guildId,
				Forgiven  = false,
				Reason    = reason,
				Moderator = modName,
			};

			var                     warnings = 1;
			List<WarningPunishment> ps;
			using(var uow = _db.UnitOfWork) {
				ps = uow.GuildConfigs.For(guildId, set => set.Include(x => x.WarnPunishments))
						.WarnPunishments;

				warnings += uow.Warnings
								.For(guildId, userId)
								.OrderByDescending(w => w.DateAdded)
								.Count(w => !w.Forgiven && w.UserId == userId);

				uow.Warnings.Add(warn);

				uow.SaveChanges();
			}

			var p = ps.FirstOrDefault(x => x.Count == warnings);

			if(p == null) return null;
			var user = await guild.GetUserAsync(userId);
			if(user == null)
				return null;
			switch(p.Punishment) {
				case PunishmentAction.Mute:
					if(p.Time == 0)
						await _mute.MuteUser(user).ConfigureAwait(false);
					else
						await _mute.TimedMute(user, TimeSpan.FromMinutes(p.Time)).ConfigureAwait(false);
					break;
				case PunishmentAction.Kick:
					await user.KickAsync().ConfigureAwait(false);
					break;
				case PunishmentAction.Ban:
					await guild.AddBanAsync(user).ConfigureAwait(false);
					break;
				case PunishmentAction.Softban:
					await guild.AddBanAsync(user, 7).ConfigureAwait(false);
					try {
						await guild.RemoveBanAsync(user).ConfigureAwait(false);
					} catch {
						await guild.RemoveBanAsync(user).ConfigureAwait(false);
					}

					break;
				default:
					break;
			}

			return p.Punishment;
		}
	}
}
