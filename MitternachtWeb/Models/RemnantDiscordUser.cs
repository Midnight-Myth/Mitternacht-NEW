using Discord;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace MitternachtWeb.Models {
	public class RemnantDiscordUser : IUser {
		// Initializable inherited properties from IUser.
		public ulong  Id                 { get; init; }
		public bool   IsBot              { get; init; } = false;
		public bool   IsWebhook          { get; init; } = false;

		// Fixed inherited properties from IUser.
		public string                    Username           => "-";
		public string                    AvatarId           => string.Empty;
		public ushort                    DiscriminatorValue => 0;
		public string                    Discriminator      => $"{DiscriminatorValue:D4}";
		public string                    Mention            => MentionUtils.MentionUser(Id);
		public IActivity                 Activity           => null;
		public DateTimeOffset            CreatedAt          => DateTimeOffset.MinValue;
		public UserStatus                Status             => UserStatus.Offline;
		public IImmutableSet<ClientType> ActiveClients      => ImmutableHashSet.Create<ClientType>();
		public IImmutableList<IActivity> Activities         => ImmutableList.Create<IActivity>();

		public UserProperties? PublicFlags { get; } = UserProperties.None;

		public RemnantDiscordUser(ulong userId) {
			Id = userId;
		}

		public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
			=> string.Empty;

		public string GetDefaultAvatarUrl()
			=> string.Empty;

		public Task<IDMChannel> CreateDMChannelAsync(RequestOptions options = null)
			=> throw new NotImplementedException();

		public override string ToString()
			=> $"{Username}#{Discriminator}";
	}
}
