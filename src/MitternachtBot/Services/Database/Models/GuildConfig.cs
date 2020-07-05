using System;
using System.Collections.Generic;

namespace Mitternacht.Services.Database.Models {
	public class GuildConfig : DbEntity {
		public ulong GuildId { get; set; }

		public string Prefix                             { get; set; } = null;
		public bool   DeleteMessageOnCommand             { get; set; }
		public ulong  AutoAssignRoleId                   { get; set; }
		public int    AutoDeleteGreetMessagesTimer       { get; set; } = 30;
		public int    AutoDeleteByeMessagesTimer         { get; set; } = 30;
		public ulong  GreetMessageChannelId              { get; set; }
		public ulong  ByeMessageChannelId                { get; set; }
		public bool   SendDmGreetMessage                 { get; set; }
		public string DmGreetMessageText                 { get; set; } = "Welcome to the %server% server, %user%!";
		public bool   SendChannelGreetMessage            { get; set; }
		public string ChannelGreetMessageText            { get; set; } = "Welcome to the %server% server, %user%!";
		public bool   SendChannelByeMessage              { get; set; }
		public string ChannelByeMessageText              { get; set; } = "%user% has left!";
		public bool   ExclusiveSelfAssignedRoles         { get; set; }
		public bool   AutoDeleteSelfAssignedRoleMessages { get; set; }
		public float  DefaultMusicVolume                 { get; set; } = 1.0f;
		public bool   VoicePlusTextEnabled               { get; set; }
		public bool   CleverbotEnabled                   { get; set; }
		public string MuteRoleName                       { get; set; }
		public string Locale                             { get; set; } = null;
		public string TimeZoneId                         { get; set; } = null;
		public ulong? GameVoiceChannel                   { get; set; } = null;
		public bool   VerboseErrors                      { get; set; } = false;
		public ulong? VerifiedRoleId                     { get; set; } = null;
		public string VerifyString                       { get; set; } = null;
		public string VerificationTutorialText           { get; set; } = null;
		public string AdditionalVerificationUsers        { get; set; } = null;
		public ulong? VerificationPasswordChannelId      { get; set; } = null;
		public double TurnToXpMultiplier                 { get; set; } = 5;
		public double MessageXpTimeDifference            { get; set; } = 60;
		public int    MessageXpCharCountMin              { get; set; } = 10;
		public int    MessageXpCharCountMax              { get; set; } = 25;
		public bool?  LogUsernameHistory                 { get; set; } = null;
		public ulong? BirthdayRoleId                     { get; set; } = null;
		public string BirthdayMessage                    { get; set; } = "Happy Birthday {0}!";
		public ulong? BirthdayMessageChannelId           { get; set; } = null;
		public bool   BirthdaysEnabled                   { get; set; } = true;
		public long?  BirthdayMoney                      { get; set; } = 50;
		public ulong? GommeTeamMemberRoleId              { get; set; } = null;
		public ulong? VipRoleId                          { get; set; } = null;
		public ulong? TeamUpdateChannelId                { get; set; } = null;
		public string TeamUpdateMessagePrefix            { get; set; } = "";
		public ulong? CountToNumberChannelId             { get; set; } = null;
		public double CountToNumberMessageChance         { get; set; } = 0.05;
		public ulong? ForumNotificationChannelId         { get; set; } = null;

		public Permission         RootPermission     { get; set; } = null;
		public List<Permissionv2> Permissions        { get; set; }
		public bool               VerbosePermissions { get; set; } = true;
		public string             PermissionRole     { get; set; } = "Permissions";

		public bool                        FilterInvites           { get; set; }
		public bool                        FilterWords             { get; set; }
		public bool                        FilterZalgo             { get; set; }
		public HashSet<FilterChannelId>    FilterInvitesChannelIds { get; set; } = new HashSet<FilterChannelId>();
		public HashSet<FilteredWord>       FilteredWords           { get; set; } = new HashSet<FilteredWord>();
		public HashSet<FilterChannelId>    FilterWordsChannelIds   { get; set; } = new HashSet<FilterChannelId>();
		public HashSet<ZalgoFilterChannel> FilterZalgoChannelIds   { get; set; } = new HashSet<ZalgoFilterChannel>();

		public LogSetting         LogSetting      { get; set; } = new LogSetting();
		public AntiRaidSetting    AntiRaidSetting { get; set; }
		public AntiSpamSetting    AntiSpamSetting { get; set; }

		public HashSet<MutedUserId>         MutedUsers                 { get; set; } = new HashSet<MutedUserId>();
		public HashSet<GuildRepeater>       GuildRepeaters             { get; set; } = new HashSet<GuildRepeater>();
		public HashSet<FollowedStream>      FollowedStreams            { get; set; } = new HashSet<FollowedStream>();
		public HashSet<GCChannelId>         GenerateCurrencyChannelIds { get; set; } = new HashSet<GCChannelId>();
		public HashSet<CommandCooldown>     CommandCooldowns           { get; set; } = new HashSet<CommandCooldown>();
		public HashSet<UnmuteTimer>         UnmuteTimers               { get; set; } = new HashSet<UnmuteTimer>();
		public HashSet<VcRoleInfo>          VcRoleInfos                { get; set; }
		public HashSet<CommandAlias>        CommandAliases             { get; set; } = new HashSet<CommandAlias>();
		public List<WarningPunishment>      WarnPunishments            { get; set; } = new List<WarningPunishment>();
		public bool                         WarningsInitialized        { get; set; }
		public HashSet<SlowmodeIgnoredUser> SlowmodeIgnoredUsers       { get; set; }
		public HashSet<SlowmodeIgnoredRole> SlowmodeIgnoredRoles       { get; set; }
		public HashSet<NsfwBlacklitedTag>   NsfwBlacklistedTags        { get; set; } = new HashSet<NsfwBlacklitedTag>();
		public List<ShopEntry>              ShopEntries                { get; set; }
	}

	public class NsfwBlacklitedTag : DbEntity {
		public string Tag { get; set; }

		public override bool Equals(object obj)
			=> obj is NsfwBlacklitedTag x && x.Tag.Equals(Tag);

		public override int GetHashCode()
			=> HashCode.Combine(Tag);
	}

	public class SlowmodeIgnoredUser : DbEntity {
		public ulong UserId { get; set; }

		public override bool Equals(object obj)
			=> obj is SlowmodeIgnoredUser siu && siu.UserId == UserId;

		public override int GetHashCode()
			=> HashCode.Combine(UserId);
	}

	public class SlowmodeIgnoredRole : DbEntity {
		public ulong RoleId { get; set; }

		public override bool Equals(object obj)
			=> obj is SlowmodeIgnoredRole sir && sir.RoleId == RoleId;

		public override int GetHashCode()
			=> HashCode.Combine(RoleId);
	}

	public class WarningPunishment : DbEntity {
		public int              Count      { get; set; }
		public int              Time       { get; set; }
		public PunishmentAction Punishment { get; set; }
	}

	public class CommandAlias : DbEntity {
		public string Trigger { get; set; }
		public string Mapping { get; set; }
	}

	public class VcRoleInfo : DbEntity {
		public ulong VoiceChannelId { get; set; }
		public ulong RoleId         { get; set; }
	}

	public class UnmuteTimer : DbEntity {
		public ulong    UserId   { get; set; }
		public DateTime UnmuteAt { get; set; }

		public override bool Equals(object obj)
			=> obj is UnmuteTimer ut && ut.UserId == UserId;

		public override int GetHashCode()
			=> HashCode.Combine(UserId);
	}

	public class FilterChannelId : DbEntity {
		public ulong ChannelId { get; set; }
	}

	public class ZalgoFilterChannel : DbEntity {
		public ulong ChannelId { get; set; }
	}

	public class FilteredWord : DbEntity {
		public string Word { get; set; }
	}

	public class MutedUserId : DbEntity {
		public ulong UserId { get; set; }

		public override bool Equals(object obj)
			=> obj is MutedUserId mui && mui.UserId == UserId;

		public override int GetHashCode()
			=> HashCode.Combine(UserId);
	}

	public class GCChannelId : DbEntity {
		public ulong ChannelId { get; set; }

		public override bool Equals(object obj)
			=> obj is GCChannelId gc && gc.ChannelId == ChannelId;

		public override int GetHashCode()
			=> HashCode.Combine(ChannelId);
	}
}
