using System.Threading.Tasks;
using Discord;

namespace Mitternacht.Extensions {
	public static class UserExtensions {
		public static async Task<IUserMessage> SendConfirmAsync(this IUser user, string text)
			 => await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("", embed: new EmbedBuilder().WithOkColor().WithDescription(text).Build());

		public static async Task<IUserMessage> SendErrorAsync(this IUser user, string error)
			 => await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("", embed: new EmbedBuilder().WithErrorColor().WithDescription(error).Build());
	}
}
