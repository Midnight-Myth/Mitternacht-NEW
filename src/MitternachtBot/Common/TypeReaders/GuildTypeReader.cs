using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Mitternacht.Common.TypeReaders {
	public class GuildTypeReader : TypeReader {
		private readonly DiscordSocketClient _client;

		public GuildTypeReader(DiscordSocketClient client) {
			_client = client;
		}

		public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services) {
			input = input.Trim();

			var guilds = _client.Guilds;
			var guild = guilds.FirstOrDefault(g => g.Id.ToString().Equals(input,StringComparison.OrdinalIgnoreCase)) ?? guilds.FirstOrDefault(g => g.Name.Equals(input, StringComparison.OrdinalIgnoreCase));

			return Task.FromResult(guild != null ? TypeReaderResult.FromSuccess(guild) : TypeReaderResult.FromError(CommandError.ParseFailed, "No guild by that name or Id found"));
		}
	}
}
