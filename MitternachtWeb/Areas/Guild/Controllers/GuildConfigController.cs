using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace MitternachtWeb.Areas.Guild.Controllers {
	[Area("Guild")]
	public class GuildConfigController : GuildBaseController {
		private readonly DbService _db;

		public GuildConfigController(DbService db) {
			_db = db;
		}

		public IActionResult Index() {
			if(PermissionReadGuildConfig) {
				using var uow = _db.UnitOfWork;

				var guildConfig = uow.GuildConfigs.For(GuildId, set => set);

				return View(guildConfig);
			} else {
				return Unauthorized();
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit([Bind("GuildId,Prefix,DeleteMessageOnCommand,AutoAssignRoleId,AutoDeleteGreetMessagesTimer,AutoDeleteByeMessagesTimer,GreetMessageChannelId,ByeMessageChannelId,SendDmGreetMessage,DmGreetMessageText,SendChannelGreetMessage,ChannelGreetMessageText,SendChannelByeMessage,ChannelByeMessageText,ExclusiveSelfAssignedRoles,AutoDeleteSelfAssignedRoleMessages,DefaultMusicVolume,VoicePlusTextEnabled,CleverbotEnabled,MuteRoleName,Locale,TimeZoneId,GameVoiceChannel,VerboseErrors,VerifiedRoleId,VerifyString,VerificationTutorialText,AdditionalVerificationUsers,VerificationPasswordChannelId,TurnToXpMultiplier,MessageXpTimeDifference,MessageXpCharCountMin,MessageXpCharCountMax,LogUsernameHistory,BirthdayRoleId,BirthdayMessage,BirthdayMessageChannelId,BirthdaysEnabled,BirthdayMoney,GommeTeamMemberRoleId,VipRoleId,TeamUpdateChannelId,TeamUpdateMessagePrefix,CountToNumberChannelId,CountToNumberMessageChance,VerbosePermissions,PermissionRole,FilterInvites,FilterWords,FilterZalgo,WarningsInitialized,ForumNotificationChannelId")] GuildConfig guildConfig) {
			if(PermissionWriteGuildConfig) {
				if(GuildId == guildConfig.GuildId) {
					if(ModelState.IsValid) {
						using var uow = _db.UnitOfWork;
						var gc        = uow.GuildConfigs.For(GuildId);

						if(gc != null) {
							gc.Prefix                             = guildConfig.Prefix;
							gc.DeleteMessageOnCommand             = guildConfig.DeleteMessageOnCommand;
							gc.AutoAssignRoleId                   = guildConfig.AutoAssignRoleId;
							gc.AutoDeleteGreetMessagesTimer       = guildConfig.AutoDeleteGreetMessagesTimer;
							gc.AutoDeleteByeMessagesTimer         = guildConfig.AutoDeleteByeMessagesTimer;
							gc.GreetMessageChannelId              = guildConfig.GreetMessageChannelId;
							gc.ByeMessageChannelId                = guildConfig.ByeMessageChannelId;
							gc.SendDmGreetMessage                 = guildConfig.SendDmGreetMessage;
							gc.DmGreetMessageText                 = guildConfig.DmGreetMessageText;
							gc.SendChannelGreetMessage            = guildConfig.SendChannelGreetMessage;
							gc.ChannelGreetMessageText            = guildConfig.ChannelGreetMessageText;
							gc.SendChannelByeMessage              = guildConfig.SendChannelByeMessage;
							gc.ChannelByeMessageText              = guildConfig.ChannelByeMessageText;
							gc.ExclusiveSelfAssignedRoles         = guildConfig.ExclusiveSelfAssignedRoles;
							gc.AutoDeleteSelfAssignedRoleMessages = guildConfig.AutoDeleteSelfAssignedRoleMessages;
							gc.DefaultMusicVolume                 = guildConfig.DefaultMusicVolume;
							gc.VoicePlusTextEnabled               = guildConfig.VoicePlusTextEnabled;
							gc.CleverbotEnabled                   = guildConfig.CleverbotEnabled;
							gc.MuteRoleName                       = guildConfig.MuteRoleName;
							gc.Locale                             = guildConfig.Locale;
							gc.TimeZoneId                         = guildConfig.TimeZoneId;
							gc.GameVoiceChannel                   = guildConfig.GameVoiceChannel;
							gc.VerboseErrors                      = guildConfig.VerboseErrors;
							gc.VerifiedRoleId                     = guildConfig.VerifiedRoleId;
							gc.VerifyString                       = guildConfig.VerifyString;
							gc.VerificationTutorialText           = guildConfig.VerificationTutorialText;
							gc.AdditionalVerificationUsers        = guildConfig.AdditionalVerificationUsers;
							gc.VerificationPasswordChannelId      = guildConfig.VerificationPasswordChannelId;
							gc.TurnToXpMultiplier                 = guildConfig.TurnToXpMultiplier;
							gc.MessageXpTimeDifference            = guildConfig.MessageXpTimeDifference;
							gc.MessageXpCharCountMin              = guildConfig.MessageXpCharCountMin;
							gc.MessageXpCharCountMax              = guildConfig.MessageXpCharCountMax;
							gc.LogUsernameHistory                 = guildConfig.LogUsernameHistory;
							gc.BirthdayRoleId                     = guildConfig.BirthdayRoleId;
							gc.BirthdayMessage                    = guildConfig.BirthdayMessage;
							gc.BirthdayMessageChannelId           = guildConfig.BirthdayMessageChannelId;
							gc.BirthdaysEnabled                   = guildConfig.BirthdaysEnabled;
							gc.BirthdayMoney                      = guildConfig.BirthdayMoney;
							gc.GommeTeamMemberRoleId              = guildConfig.GommeTeamMemberRoleId;
							gc.VipRoleId                          = guildConfig.VipRoleId;
							gc.TeamUpdateChannelId                = guildConfig.TeamUpdateChannelId;
							gc.TeamUpdateMessagePrefix            = guildConfig.TeamUpdateMessagePrefix;
							gc.CountToNumberChannelId             = guildConfig.CountToNumberChannelId;
							gc.CountToNumberMessageChance         = guildConfig.CountToNumberMessageChance;
							gc.VerbosePermissions                 = guildConfig.VerbosePermissions;
							gc.PermissionRole                     = guildConfig.PermissionRole;
							gc.FilterInvites                      = guildConfig.FilterInvites;
							gc.FilterWords                        = guildConfig.FilterWords;
							gc.FilterZalgo                        = guildConfig.FilterZalgo;
							gc.WarningsInitialized                = guildConfig.WarningsInitialized;
							gc.ForumNotificationChannelId         = guildConfig.ForumNotificationChannelId;

							uow.GuildConfigs.Update(gc);

							await uow.CompleteAsync();
							return RedirectToAction(nameof(Index));
						} else {
							return NotFound();
						}
					} else {
						return View(guildConfig);
					}
				} else {
					return NotFound();
				}
			} else {
				return Unauthorized();
			}
		}
	}
}
