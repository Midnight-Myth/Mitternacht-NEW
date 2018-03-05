using System;
using System.Collections.Generic;

namespace Mitternacht.Services.Database.Models
{
    public class GuildConfig : DbEntity
    {
        public ulong GuildId { get; set; }

        public string Prefix { get; set; } = null;

        public bool DeleteMessageOnCommand { get; set; }
        public ulong AutoAssignRoleId { get; set; }
        //greet stuff
        public bool AutoDeleteGreetMessages { get; set; } //unused
        public bool AutoDeleteByeMessages { get; set; } // unused
        public int AutoDeleteGreetMessagesTimer { get; set; } = 30;
        public int AutoDeleteByeMessagesTimer { get; set; } = 30;

        public ulong GreetMessageChannelId { get; set; }
        public ulong ByeMessageChannelId { get; set; }

        public bool SendDmGreetMessage { get; set; }
        public string DmGreetMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public bool SendChannelGreetMessage { get; set; }
        public string ChannelGreetMessageText { get; set; } = "Welcome to the %server% server, %user%!";

        public bool SendChannelByeMessage { get; set; }
        public string ChannelByeMessageText { get; set; } = "%user% has left!";

        public LogSetting LogSetting { get; set; } = new LogSetting();

        //self assignable roles
        public bool ExclusiveSelfAssignedRoles { get; set; }
        public bool AutoDeleteSelfAssignedRoleMessages { get; set; }
        public float DefaultMusicVolume { get; set; } = 1.0f;
        public bool VoicePlusTextEnabled { get; set; }

        //stream notifications
        public HashSet<FollowedStream> FollowedStreams { get; set; } = new HashSet<FollowedStream>();

        //currencyGeneration
        public HashSet<GCChannelId> GenerateCurrencyChannelIds { get; set; } = new HashSet<GCChannelId>();

        //permissions
        public Permission RootPermission { get; set; } = null;
        public List<Permissionv2> Permissions { get; set; }
        public bool VerbosePermissions { get; set; } = true;
        public string PermissionRole { get; set; } = "Permissions";

        public HashSet<CommandCooldown> CommandCooldowns { get; set; } = new HashSet<CommandCooldown>();

        //filtering
        public bool FilterInvites { get; set; }
        public HashSet<FilterChannelId> FilterInvitesChannelIds { get; set; } = new HashSet<FilterChannelId>();

        public bool FilterWords { get; set; }
        public HashSet<FilteredWord> FilteredWords { get; set; } = new HashSet<FilteredWord>();
        public HashSet<FilterChannelId> FilterWordsChannelIds { get; set; } = new HashSet<FilterChannelId>();

        public bool FilterZalgo { get; set; }
        public HashSet<ZalgoFilterChannel> FilterZalgoChannelIds { get; set; } = new HashSet<ZalgoFilterChannel>();

        public HashSet<MutedUserId> MutedUsers { get; set; } = new HashSet<MutedUserId>();

        public string MuteRoleName { get; set; }
        public bool CleverbotEnabled { get; set; }
        public HashSet<GuildRepeater> GuildRepeaters { get; set; } = new HashSet<GuildRepeater>();

        public AntiRaidSetting AntiRaidSetting { get; set; }
        public AntiSpamSetting AntiSpamSetting { get; set; }

        public string Locale { get; set; } = null;
        public string TimeZoneId { get; set; } = null;

        public HashSet<UnmuteTimer> UnmuteTimers { get; set; } = new HashSet<UnmuteTimer>();
        public HashSet<VcRoleInfo> VcRoleInfos { get; set; }
        public HashSet<CommandAlias> CommandAliases { get; set; } = new HashSet<CommandAlias>();
        public List<WarningPunishment> WarnPunishments { get; set; } = new List<WarningPunishment>();
        public bool WarningsInitialized { get; set; }
        public HashSet<SlowmodeIgnoredUser> SlowmodeIgnoredUsers { get; set; }
        public HashSet<SlowmodeIgnoredRole> SlowmodeIgnoredRoles { get; set; }
        public HashSet<NsfwBlacklitedTag> NsfwBlacklistedTags { get; set; } = new HashSet<NsfwBlacklitedTag>();

        public List<ShopEntry> ShopEntries { get; set; }
        public ulong? GameVoiceChannel { get; set; } = null;
        public bool VerboseErrors { get; set; } = false;

        public StreamRoleSettings StreamRole { get; set; }

        //public List<ProtectionIgnoredChannel> ProtectionIgnoredChannels { get; set; } = new List<ProtectionIgnoredChannel>();
        [Obsolete]
        public ulong? SupportChannelId { get; set; }

        public ulong? VerifiedRoleId { get; set; } = null;
        public string VerifyString { get; set; } = null;
        public string VerificationTutorialText { get; set; } = null;
        public string AdditionalVerificationUsers { get; set; } = null;
        public double TurnToXpMultiplier { get; set; } = 5;
        public double MessageXpTimeDifference { get; set; } = 60;
        public int MessageXpCharCountMin { get; set; } = 10;
        public int MessageXpCharCountMax { get; set; } = 25;
        public bool? LogUsernameHistory { get; set; } = null;

        public ulong? BirthdayRoleId { get; set; } = null;
    }

    public class NsfwBlacklitedTag : DbEntity
    {
        public string Tag { get; set; }

        public override int GetHashCode()
        {
            return Tag.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is NsfwBlacklitedTag x && x.Tag == Tag;
        }
    }

    public class SlowmodeIgnoredUser : DbEntity
    {
        public ulong UserId { get; set; }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return ((SlowmodeIgnoredUser)obj).UserId == UserId;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return UserId.GetHashCode();
        }
    }

    public class SlowmodeIgnoredRole : DbEntity
    {
        public ulong RoleId { get; set; }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return ((SlowmodeIgnoredRole)obj).RoleId == RoleId;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return RoleId.GetHashCode();
        }
    }

    public class WarningPunishment : DbEntity
    {
        public int Count { get; set; }
        public PunishmentAction Punishment { get; set; }
        public int Time { get; set; }
    }

    public class CommandAlias : DbEntity
    {
        public string Trigger { get; set; }
        public string Mapping { get; set; }

        //// override object.Equals
        //public override bool Equals(object obj)
        //{
        //    if (obj == null || GetType() != obj.GetType())
        //    {
        //        return false;
        //    }

        //    return ((CommandAlias)obj).Trigger.Trim().ToLowerInvariant() == Trigger.Trim().ToLowerInvariant();
        //}

        //// override object.GetHashCode
        //public override int GetHashCode()
        //{
        //    return Trigger.Trim().ToLowerInvariant().GetHashCode();
        //}
    }

    public class VcRoleInfo : DbEntity
    {
        public ulong VoiceChannelId { get; set; }
        public ulong RoleId { get; set; }
    }

    public class UnmuteTimer : DbEntity
    {
        public ulong UserId { get; set; }
        public DateTime UnmuteAt { get; set; }

        public override int GetHashCode() =>
            UserId.GetHashCode();

        public override bool Equals(object obj)
        {
            if (!(obj is UnmuteTimer ut))
                return false;
            return ut.UserId == UserId;
        }
    }

    public class FilterChannelId : DbEntity
    {
        public ulong ChannelId { get; set; }
    }

    public class ZalgoFilterChannel : DbEntity
    {
        public ulong ChannelId { get; set; }
    }

    public class FilteredWord : DbEntity
    {
        public string Word { get; set; }
    }

    public class MutedUserId : DbEntity
    {
        public ulong UserId { get; set; }

        public override int GetHashCode()
        {
            return UserId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MutedUserId mui))
                return false;

            return mui.UserId == UserId;
        }
    }

    public class GCChannelId : DbEntity
    {
        public ulong ChannelId { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is GCChannelId gc))
                return false;

            return gc.ChannelId == ChannelId;
        }

        public override int GetHashCode() =>
            ChannelId.GetHashCode();
    }
}
