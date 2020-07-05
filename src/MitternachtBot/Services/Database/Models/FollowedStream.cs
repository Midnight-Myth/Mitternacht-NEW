using System;

namespace Mitternacht.Services.Database.Models {
	public class FollowedStream : DbEntity {
		public ulong              ChannelId { get; set; }
		public string             Username  { get; set; }
		public FollowedStreamType Type      { get; set; }
		public ulong              GuildId   { get; set; }

		public enum FollowedStreamType {
			Twitch,
			Smashcast,
			Mixer
		}

		public override int GetHashCode()
			=> ChannelId.GetHashCode() ^ Username.GetHashCode() ^ Type.GetHashCode();

		public override bool Equals(object obj)
			=> obj is FollowedStream fs && fs.ChannelId == ChannelId && fs.Username.Trim().Equals(Username.Trim(), StringComparison.OrdinalIgnoreCase) && fs.Type == Type;
	}
}