using System.Collections.Generic;

namespace Mitternacht.Database.Models {
	public class LogSetting : DbEntity {
		public HashSet<IgnoredLogChannel>           IgnoredChannels                { get; set; } = new HashSet<IgnoredLogChannel>();
		public HashSet<IgnoredVoicePresenceChannel> IgnoredVoicePresenceChannelIds { get; set; } = new HashSet<IgnoredVoicePresenceChannel>();

		public ulong? LogOtherId            { get; set; }
		public ulong? MessageUpdatedId      { get; set; }
		public ulong? MessageDeletedId      { get; set; }

		public ulong? UserJoinedId          { get; set; }
		public ulong? UserLeftId            { get; set; }
		public ulong? UserBannedId          { get; set; }
		public ulong? UserUnbannedId        { get; set; }
		public ulong? UserUpdatedId         { get; set; }
		public ulong? UserMutedId           { get; set; }

		public ulong? ChannelCreatedId      { get; set; }
		public ulong? ChannelDestroyedId    { get; set; }
		public ulong? ChannelUpdatedId      { get; set; }

		public ulong? LogUserPresenceId     { get; set; }

		public ulong? LogVoicePresenceId    { get; set; }
		public ulong? LogVoicePresenceTTSId { get; set; }

		public ulong? VerificationSteps     { get; set; }
		public ulong? VerificationMessages  { get; set; }
	}
}