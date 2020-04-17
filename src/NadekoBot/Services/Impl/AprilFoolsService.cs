using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Mitternacht.Extensions;

namespace Mitternacht.Services.Impl {
	public class AprilFoolsService : IMService {
		private readonly DbService _db;
		private readonly Random _rnd = new Random();

		public AprilFoolsService(CommandHandler commandHandler, DbService db) {
			_db = db;

			commandHandler.OnValidMessage += OnValidMessage;
		}

		private async Task OnValidMessage(SocketUserMessage message) {
			if(!DateTime.Now.IsOtherDate(new DateTime(2018, 04, 01), ignoreYear: true)) {
				using var uow = _db.UnitOfWork;
				var bc = uow.BotConfig.GetOrCreate();
				if(_rnd.NextDouble() < bc.FirstAprilHereChance)
					await message.Channel.SendMessageAsync($"April April @here! Ich habe auf die Nachricht von {message.Author.Mention} reagiert.").ConfigureAwait(false);
			}
		}
	}
}
