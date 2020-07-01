using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Extensions;
using Mitternacht.Services;
using NLog;


namespace Mitternacht.Modules.Birthday.Services {
	public class BirthdayService : IMService {
		private readonly DbService _db;
		private readonly DiscordSocketClient _client;
		private readonly Logger _log;

		public event Func<SocketGuildUser[], Task> UserBirthdayStarting = u => Task.CompletedTask;
		public event Func<SocketGuildUser[], Task> BirthdayUsers = u => Task.CompletedTask;

		public BirthdayService(DbService db, DiscordSocketClient client) {
			_db = db;
			_client = client;

			_log = LogManager.GetCurrentClassLogger();

			var _ = Task.Run(async () => {
				var log = LogManager.GetCurrentClassLogger();
				while(true) {
					try {
						await TimerHandler();
					} catch(Exception e) {
						log.Warn(e);
					}
					await Task.Delay(TimeConstants.Birthday);
				}
			});

			UserBirthdayStarting += OnUserBirthdayStarting;
			UserBirthdayStarting += OnBirthdayMoney;
			BirthdayUsers += OnBirthdayUsers;
		}

		private async Task TimerHandler() {
			using var uow = _db.UnitOfWork;
			var time = DateTime.Now;
			//var time = CustomTimeForTesting;
			var birthdays = uow.BirthDates.GetBirthdays(time).ToList();
			var bc = uow.BotConfig.GetOrCreate();
			var newDay = bc.LastTimeBirthdaysChecked.IsOtherDate(time);
			bc.LastTimeBirthdaysChecked = time;
			uow.BotConfig.Update(bc);
			await uow.CompleteAsync().ConfigureAwait(false);

			//event logic
			var guildConfigs = uow.GuildConfigs.GetAllGuildConfigs(_client.Guilds.Select(g => g.Id).ToList()).Where(gc => gc.BirthdaysEnabled).ToList();
			var birthdayUsers = guildConfigs.SelectMany(gc => birthdays.Select(b => _client.GetGuild(gc.GuildId).GetUser(b.UserId)).Where(u => u != null).ToList()).ToArray();

			await BirthdayUsers.Invoke(birthdayUsers).ConfigureAwait(false);
			if(newDay && birthdayUsers.Any())
				await UserBirthdayStarting.Invoke(birthdayUsers).ConfigureAwait(false);
		}

		private async Task OnUserBirthdayStarting(SocketGuildUser[] birthdayUsers) {
			using var uow = _db.UnitOfWork;
			var usersGuildGroups = birthdayUsers.Where(u => uow.BirthDates.BirthdayMessageEnabled(u.Id).Value).GroupBy(u => u.Guild).ToList();

			foreach(var group in usersGuildGroups) {
				var guild = group.Key;
				var gc = uow.GuildConfigs.For(guild.Id);

				var msgChannelId = gc.BirthdayMessageChannelId;
				if(!msgChannelId.HasValue)
					continue;
				var ch = guild.GetTextChannel(msgChannelId.Value);

				var msg = gc.BirthdayMessage;

				if(ch != null)
					await ch.SendMessageAsync(string.Format(msg, string.Join(", ", group.Select(u => u.Mention).ToList()))).ConfigureAwait(false);
			}
		}

		private async Task OnBirthdayUsers(SocketGuildUser[] birthdayUsers) {
			using var uow = _db.UnitOfWork;
			var guildConfigs = uow.GuildConfigs.GetAllGuildConfigs(_client.Guilds.Select(g => g.Id).ToList()).Where(gc => gc.BirthdaysEnabled).ToList();

			foreach(var gc in guildConfigs) {
				var guild = _client.GetGuild(gc.GuildId);
				var birthdayRoleId = gc.BirthdayRoleId;
				if(!birthdayRoleId.HasValue)
					continue;

				var birthdayRole = guild.GetRole(birthdayRoleId.Value);
				if(birthdayRole == null)
					continue;

				var oldBirthdayRoleMembers = (await birthdayRole.GetMembersAsync().ConfigureAwait(false)).ToList();
				var guildBirthdayUsers = birthdayUsers.Where(bu => bu.Guild.Id == gc.GuildId).ToList();

				//Remove birthday role from all users who currently have it
				foreach(var guildUser in oldBirthdayRoleMembers.Where(u => guildBirthdayUsers.All(gu => gu.Id != u.Id)).ToList()) {
					await guildUser.RemoveRoleAsync(birthdayRole).ConfigureAwait(false);
				}

				//add birthday role to everyone having birthday
				foreach(var guildUser in guildBirthdayUsers.Where(bu => oldBirthdayRoleMembers.All(obrm => obrm.Id != bu.Id)).ToList()) {
					await guildUser.AddRoleAsync(birthdayRole).ConfigureAwait(false);
				}
			}
		}

		private async Task OnBirthdayMoney(SocketGuildUser[] birthdayUsers) {
			using var uow = _db.UnitOfWork;
			var usersGuildGroups = birthdayUsers.GroupBy(bu => bu.Guild).ToList();

			foreach(var group in usersGuildGroups) {
				var gc = uow.GuildConfigs.For(group.Key.Id);
				var birthdayMoney = gc.BirthdayMoney;
				if(!birthdayMoney.HasValue || birthdayMoney.Value == 0)
					continue;

				foreach(var guildUser in group) {
					var success = uow.Currency.TryUpdateState(guildUser.Id, birthdayMoney.Value);
					if(!success)
						_log.Warn($"Failed to add BirthdayMoney to {guildUser.Username} on guild {guildUser.Guild.Name}");
				}
			}
			await uow.CompleteAsync().ConfigureAwait(false);
		}

		//private int _customTimeAccessCount = 0;
		//private DateTime CustomTimeForTesting
		//{
		//    get {
		//        using (var uow = _db.UnitOfWork)
		//        {
		//            var bc = uow.BotConfig.GetOrCreate();
		//            bc.LastTimeBirthdaysChecked = _customTimeAccessCount % 2 == 0 ? DateTime.Today : DateTime.MinValue;
		//            var time = _customTimeAccessCount % 2 == 0 ? DateTime.Today.AddDays(2) : DateTime.Today;
		//            uow.BotConfig.Update(bc);
		//            uow.Complete();
		//            _customTimeAccessCount++;
		//            return time;
		//        }
		//    }
		//}
	}
}