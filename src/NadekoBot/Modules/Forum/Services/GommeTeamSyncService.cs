using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GommeHDnetForumAPI.DataModels;
using GommeHDnetForumAPI.DataModels.Entities;
using Mitternacht.Modules.Verification.Services;
using Mitternacht.Services;

namespace Mitternacht.Modules.Forum.Services {
	public class TeamRoleSyncService : INService {
		private readonly DbService           _db;
		private readonly ForumService        _fs;
		private readonly DiscordSocketClient _client;

		public TeamRoleSyncService(DbService db, ForumService fs, TeamUpdateService tus, DiscordSocketClient client, VerificationService vs) {
			_db     = db;
			_fs     = fs;
			_client = client;

			tus.TeamMemberAdded   += OnTeamMemberAdded;
			tus.TeamMemberRemoved += OnTeamMemberRemoved;
			vs.UserVerified       += OnUserVerified;
		}

		private async Task OnUserVerified(IGuildUser user, long forumUserId) {
			using(var uow = _db.UnitOfWork) {
				var gc            = uow.GuildConfigs.For(user.GuildId);
				var gommeTeamRole = gc.GommeTeamMemberRoleId.HasValue ? user.Guild.GetRole(gc.GommeTeamMemberRoleId.Value) : null;
				if(gommeTeamRole != null) {
					var staffList = await _fs.Forum.GetMembersList(MembersListType.Staff);
					if(staffList.Any(s => s.Id == forumUserId)) {
						await user.AddRoleAsync(gommeTeamRole).ConfigureAwait(false);
					}
				}
			}
		}

		private async Task OnTeamMemberAdded(UserInfo[] userInfos)
			=> await TeamMemberRoleChange(userInfos, true);

		private async Task OnTeamMemberRemoved(UserInfo[] userInfos)
			=> await TeamMemberRoleChange(userInfos, false);

		private async Task TeamMemberRoleChange(UserInfo[] userInfos, bool addGommeTeamRole) {
			using(var uow = _db.UnitOfWork) {
				var guildConfigs = uow.GuildConfigs.GetAllGuildConfigs(_client.Guilds.Select(sg => sg.Id).ToList());

				foreach(var gc in guildConfigs) {
					var guild         = _client.GetGuild(gc.GuildId);
					var gommeTeamRole = gc.GommeTeamMemberRoleId.HasValue ? guild.GetRole(gc.GommeTeamMemberRoleId.Value) : null;
					if(gommeTeamRole == null) continue;
					var vipRole = gc.VipRoleId.HasValue ? guild.GetRole(gc.VipRoleId.Value) : null;

					var verifiedUsers = userInfos.Select(ui => uow.VerifiedUsers.GetVerifiedUserId(guild.Id, ui.Id)).Select(uid => uid.HasValue ? guild.GetUser(uid.Value) : null).Where(gu => gu != null).ToList();
					if(addGommeTeamRole) {
						foreach(var user in verifiedUsers) {
							await user.AddRoleAsync(gommeTeamRole).ConfigureAwait(false);
						}
					} else {
						foreach(var user in verifiedUsers) {
							await user.RemoveRoleAsync(gommeTeamRole).ConfigureAwait(false);
							if(vipRole != null) await user.AddRoleAsync(vipRole).ConfigureAwait(false);
						}
					}
				}
			}
		}
	}
}