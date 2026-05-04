using Mitternacht.Database.Models;

namespace MitternachtWeb.Areas.Guild.Models {
	public class EditGuildConfig {
		public ulong GuildId { get; set; }

		public string Prefix { get; set; }
		public bool DeleteMessageOnCommand { get; set; }
		public ulong AutoAssignRoleId { get; set; }
		public int AutoDeleteGreetMessagesTimer { get; set; }
		public int AutoDeleteByeMessagesTimer { get; set; }
		public ulong GreetMessageChannelId { get; set; }
		public ulong ByeMessageChannelId { get; set; }
		public bool SendDmGreetMessage { get; set; }
		public string DmGreetMessageText { get; set; }
		public bool SendChannelGreetMessage { get; set; }
		public string ChannelGreetMessageText { get; set; }
		public bool SendChannelByeMessage { get; set; }
		public string ChannelByeMessageText { get; set; }
		public bool ExclusiveSelfAssignedRoles { get; set; }
		public bool AutoDeleteSelfAssignedRoleMessages { get; set; }
		public bool VoicePlusTextEnabled { get; set; }
		public string MuteRoleName { get; set; }
		public ulong? MutedRoleId { get; set; }
		public ulong? SilencedRoleId { get; set; }
		public string Locale { get; set; }
		public string TimeZoneId { get; set; }
		public ulong? GameVoiceChannel { get; set; }
		public bool VerboseErrors { get; set; }
		public ulong? VerifiedRoleId { get; set; }
		public string VerifyString { get; set; }
		public string VerificationTutorialText { get; set; }
		public string AdditionalVerificationUsers { get; set; }
		public ulong? VerificationPasswordChannelId { get; set; }
		public double TurnToXpMultiplier { get; set; }
		public double MessageXpTimeDifference { get; set; }
		public int MessageXpCharCountMin { get; set; }
		public int MessageXpCharCountMax { get; set; }
		public bool? LogUsernameHistory { get; set; }
		public ulong? BirthdayRoleId { get; set; }
		public string BirthdayMessage { get; set; }
		public ulong? BirthdayMessageChannelId { get; set; }
		public bool BirthdaysEnabled { get; set; }
		public long? BirthdayMoney { get; set; }
		public ulong? GommeTeamMemberRoleId { get; set; }
		public ulong? VipRoleId { get; set; }
		public ulong? TeamUpdateChannelId { get; set; }
		public string TeamUpdateMessagePrefix { get; set; }
		public ulong? CountToNumberChannelId { get; set; }
		public double CountToNumberMessageChance { get; set; }
		public bool CountToNumberDeleteWrongMessages { get; set; }
		public ulong? ForumNotificationChannelId { get; set; }
		public double ColorMetricSimilarityRadius { get; set; }
		public ulong? ForumAccountWatchNotificationChannelId { get; set; }

		public bool VerbosePermissions { get; set; }
		public string PermissionRole { get; set; }
		public bool FilterInvites { get; set; }
		public bool FilterWords { get; set; }
		public bool FilterZalgo { get; set; }
		public bool WarningsInitialized { get; set; }

		public bool ApplyToGuildConfig(GuildConfig guildConfig) {
			if(GuildId == guildConfig.GuildId) {
				guildConfig.Prefix = Prefix;
				guildConfig.DeleteMessageOnCommand = DeleteMessageOnCommand;
				guildConfig.AutoAssignRoleId = AutoAssignRoleId;
				guildConfig.AutoDeleteGreetMessagesTimer = AutoDeleteGreetMessagesTimer;
				guildConfig.AutoDeleteByeMessagesTimer = AutoDeleteByeMessagesTimer;
				guildConfig.GreetMessageChannelId = GreetMessageChannelId;
				guildConfig.ByeMessageChannelId = ByeMessageChannelId;
				guildConfig.SendDmGreetMessage = SendDmGreetMessage;
				guildConfig.DmGreetMessageText = DmGreetMessageText;
				guildConfig.SendChannelGreetMessage = SendChannelGreetMessage;
				guildConfig.ChannelGreetMessageText = ChannelGreetMessageText;
				guildConfig.SendChannelByeMessage = SendChannelByeMessage;
				guildConfig.ChannelByeMessageText = ChannelByeMessageText;
				guildConfig.ExclusiveSelfAssignedRoles = ExclusiveSelfAssignedRoles;
				guildConfig.AutoDeleteSelfAssignedRoleMessages = AutoDeleteSelfAssignedRoleMessages;
				guildConfig.VoicePlusTextEnabled = VoicePlusTextEnabled;
				guildConfig.MuteRoleName = MuteRoleName;
				guildConfig.MutedRoleId = MutedRoleId;
				guildConfig.SilencedRoleId = SilencedRoleId;
				guildConfig.Locale = Locale;
				guildConfig.TimeZoneId = TimeZoneId;
				guildConfig.GameVoiceChannel = GameVoiceChannel;
				guildConfig.VerboseErrors = VerboseErrors;
				guildConfig.VerifiedRoleId = VerifiedRoleId;
				guildConfig.VerifyString = VerifyString;
				guildConfig.VerificationTutorialText = VerificationTutorialText;
				guildConfig.AdditionalVerificationUsers = AdditionalVerificationUsers;
				guildConfig.VerificationPasswordChannelId = VerificationPasswordChannelId;
				guildConfig.TurnToXpMultiplier = TurnToXpMultiplier;
				guildConfig.MessageXpTimeDifference = MessageXpTimeDifference;
				guildConfig.MessageXpCharCountMin = MessageXpCharCountMin;
				guildConfig.MessageXpCharCountMax = MessageXpCharCountMax;
				guildConfig.LogUsernameHistory = LogUsernameHistory;
				guildConfig.BirthdayRoleId = BirthdayRoleId;
				guildConfig.BirthdayMessage = BirthdayMessage;
				guildConfig.BirthdayMessageChannelId = BirthdayMessageChannelId;
				guildConfig.BirthdaysEnabled = BirthdaysEnabled;
				guildConfig.BirthdayMoney = BirthdayMoney;
				guildConfig.GommeTeamMemberRoleId = GommeTeamMemberRoleId;
				guildConfig.VipRoleId = VipRoleId;
				guildConfig.TeamUpdateChannelId = TeamUpdateChannelId;
				guildConfig.TeamUpdateMessagePrefix = TeamUpdateMessagePrefix;
				guildConfig.CountToNumberChannelId = CountToNumberChannelId;
				guildConfig.CountToNumberMessageChance = CountToNumberMessageChance;
				guildConfig.CountToNumberDeleteWrongMessages = CountToNumberDeleteWrongMessages;
				guildConfig.ForumNotificationChannelId = ForumNotificationChannelId;
				guildConfig.ColorMetricSimilarityRadius = ColorMetricSimilarityRadius;
				guildConfig.ForumAccountWatchNotificationChannelId = ForumAccountWatchNotificationChannelId;
				guildConfig.VerbosePermissions = VerbosePermissions;
				guildConfig.PermissionRole = PermissionRole;
				guildConfig.FilterInvites = FilterInvites;
				guildConfig.FilterWords = FilterWords;
				guildConfig.FilterZalgo = FilterZalgo;
				guildConfig.WarningsInitialized = WarningsInitialized;

				return true;
			} else {
				return false;
			}
		}

		public static EditGuildConfig FromGuildConfig(GuildConfig guildConfig) {
			return new EditGuildConfig {
				GuildId = guildConfig.GuildId,

				Prefix = guildConfig.Prefix,
				DeleteMessageOnCommand = guildConfig.DeleteMessageOnCommand,
				AutoAssignRoleId = guildConfig.AutoAssignRoleId,
				AutoDeleteGreetMessagesTimer = guildConfig.AutoDeleteGreetMessagesTimer,
				AutoDeleteByeMessagesTimer = guildConfig.AutoDeleteByeMessagesTimer,
				GreetMessageChannelId = guildConfig.GreetMessageChannelId,
				ByeMessageChannelId = guildConfig.ByeMessageChannelId,
				SendDmGreetMessage = guildConfig.SendDmGreetMessage,
				DmGreetMessageText = guildConfig.DmGreetMessageText,
				SendChannelGreetMessage = guildConfig.SendChannelGreetMessage,
				ChannelGreetMessageText = guildConfig.ChannelGreetMessageText,
				SendChannelByeMessage = guildConfig.SendChannelByeMessage,
				ChannelByeMessageText = guildConfig.ChannelByeMessageText,
				ExclusiveSelfAssignedRoles = guildConfig.ExclusiveSelfAssignedRoles,
				AutoDeleteSelfAssignedRoleMessages = guildConfig.AutoDeleteSelfAssignedRoleMessages,
				VoicePlusTextEnabled = guildConfig.VoicePlusTextEnabled,
				MuteRoleName = guildConfig.MuteRoleName,
				MutedRoleId = guildConfig.MutedRoleId,
				SilencedRoleId = guildConfig.SilencedRoleId,
				Locale = guildConfig.Locale,
				TimeZoneId = guildConfig.TimeZoneId,
				GameVoiceChannel = guildConfig.GameVoiceChannel,
				VerboseErrors = guildConfig.VerboseErrors,
				VerifiedRoleId = guildConfig.VerifiedRoleId,
				VerifyString = guildConfig.VerifyString,
				VerificationTutorialText = guildConfig.VerificationTutorialText,
				AdditionalVerificationUsers = guildConfig.AdditionalVerificationUsers,
				VerificationPasswordChannelId = guildConfig.VerificationPasswordChannelId,
				TurnToXpMultiplier = guildConfig.TurnToXpMultiplier,
				MessageXpTimeDifference = guildConfig.MessageXpTimeDifference,
				MessageXpCharCountMin = guildConfig.MessageXpCharCountMin,
				MessageXpCharCountMax = guildConfig.MessageXpCharCountMax,
				LogUsernameHistory = guildConfig.LogUsernameHistory,
				BirthdayRoleId = guildConfig.BirthdayRoleId,
				BirthdayMessage = guildConfig.BirthdayMessage,
				BirthdayMessageChannelId = guildConfig.BirthdayMessageChannelId,
				BirthdaysEnabled = guildConfig.BirthdaysEnabled,
				BirthdayMoney = guildConfig.BirthdayMoney,
				GommeTeamMemberRoleId = guildConfig.GommeTeamMemberRoleId,
				VipRoleId = guildConfig.VipRoleId,
				TeamUpdateChannelId = guildConfig.TeamUpdateChannelId,
				TeamUpdateMessagePrefix = guildConfig.TeamUpdateMessagePrefix,
				CountToNumberChannelId = guildConfig.CountToNumberChannelId,
				CountToNumberMessageChance = guildConfig.CountToNumberMessageChance,
				CountToNumberDeleteWrongMessages = guildConfig.CountToNumberDeleteWrongMessages,
				ForumNotificationChannelId = guildConfig.ForumNotificationChannelId,
				ColorMetricSimilarityRadius = guildConfig.ColorMetricSimilarityRadius,
				ForumAccountWatchNotificationChannelId = guildConfig.ForumAccountWatchNotificationChannelId,
				VerbosePermissions = guildConfig.VerbosePermissions,
				PermissionRole = guildConfig.PermissionRole,
				FilterInvites = guildConfig.FilterInvites,
				FilterWords = guildConfig.FilterWords,
				FilterZalgo = guildConfig.FilterZalgo,
				WarningsInitialized = guildConfig.WarningsInitialized,
			};
		}
	}
}
