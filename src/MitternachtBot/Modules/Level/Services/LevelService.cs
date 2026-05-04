using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Database.Repositories;
using Mitternacht.Database.Repositories.Impl;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Level.Services {
	public class LevelService : IMService {
		private readonly DiscordSocketClient _client;
		private readonly DbService           _db;
		private readonly CommandHandler      _ch;

		public LevelService(DiscordSocketClient client, DbService db, CommandHandler ch) {
			_client                           =  client;
			_db                               =  db;
			_ch                               =  ch;
			_ch.OnMessageNoTrigger            += OnMessageNoTrigger;
			client.MessageDeleted             += OnMessageDeleted;
			client.MessageReceived            += AddLevelRole;
			LevelModelRepository.LevelChanged += SendLevelChangedMessage;
		}

		private async Task AddLevelRole(SocketMessage sm) {
			await Task.Run(async () => {
				if(!(sm.Author is IGuildUser user))
					return;

				using var uow = _db.UnitOfWork;
				var level     = uow.LevelModel.Get(user.GuildId, user.Id)?.Level ?? 0;
				var userroles = user.GetRoles().ToArray();
				var rlb       = uow.RoleLevelBindings.GetAll().Where(rl => rl.MinimumLevel <= level).AsEnumerable().Where(rl => userroles.All(ur => ur.Id != rl.RoleId)).ToList();
				var rolesToAdd = user.Guild.Roles.Where(r => rlb.Any(rs => rs.RoleId == r.Id)).OrderBy(r => r.Position).ToList();

				if(!rolesToAdd.Any())
					return;
				var rolestring = rolesToAdd.Aggregate("\"", (s, r) => $"{s}{r.Name}\", \"", s => s.Substring(0, s.Length - 3));
				await user.AddRolesAsync(rolesToAdd).ConfigureAwait(false);
				await sm.Channel.SendMessageAsync($"{user.Mention} hat die Rolle{(rolesToAdd.Count > 1 ? "n" : "")} {rolestring} bekommen.").ConfigureAwait(false);
			}).ConfigureAwait(false);
		}

		private async Task OnMessageNoTrigger(IUserMessage um) {
			await Task.Run(async () => {
				if(!(um.Author is IGuildUser user) || um.Channel is IThreadChannel || um.Channel is IForumChannel)
					return;
				using var uow = _db.UnitOfWork;
				if(uow.MessageXpRestrictions.IsRestricted(um.Channel as ITextChannel) || um.Content.Length < uow.GuildConfigs.For(user.GuildId).MessageXpCharCountMin)
					return;

				var time = DateTime.UtcNow;
				if(uow.LevelModel.CanGetMessageXP(user.GuildId, user.Id, time)) {
					var maxXp = uow.GuildConfigs.For(user.GuildId).MessageXpCharCountMax;
					uow.LevelModel.AddXP(user.GuildId, user.Id, um.Content.Length > maxXp ? maxXp : um.Content.Length, um.Channel.Id);
					uow.LevelModel.ReplaceTimestampOfLastMessageXP(user.GuildId, user.Id, time);
				}

				await uow.SaveChangesAsync().ConfigureAwait(false);
			}).ConfigureAwait(false);
		}

		private async Task OnMessageDeleted(Cacheable<IMessage, ulong> before, Cacheable<IMessageChannel, ulong> channel) {
			var msg = await before.GetOrDownloadAsync().ConfigureAwait(false);
			if(msg == null || !(msg.Author is IGuildUser user) || await _ch.WouldGetExecuted(msg).ConfigureAwait(false))
				return;

			using var uow = _db.UnitOfWork;
			if(uow.MessageXpRestrictions.IsRestricted(await channel.GetOrDownloadAsync() as ITextChannel))
				return;
			uow.LevelModel.AddXP(user.GuildId, user.Id, -uow.GuildConfigs.For(user.GuildId).MessageXpCharCountMax, channel.Id);
			await uow.SaveChangesAsync().ConfigureAwait(false);
		}

		private async Task SendLevelChangedMessage(LevelChangedArgs lc) {
			await Task.Run(async () => {
				if(lc.ChannelId == null)
					return;
				var channel = _client.GetGuild(lc.GuildId)?.GetTextChannel(lc.ChannelId.Value);
				if(channel == null)
					return;

				if(lc.ChangeType == LevelChangedArgs.ChangeTypes.Up)
					await channel.SendConfirmAsync($"Herzlichen Glückwunsch {MentionUtils.MentionUser(lc.UserId)}, du bist von Level {lc.OldLevel} auf Level {lc.NewLevel} aufgestiegen!").ConfigureAwait(false);
				else if(lc.ChangeType == LevelChangedArgs.ChangeTypes.Down)
					await channel.SendConfirmAsync($"Schade {MentionUtils.MentionUser(lc.UserId)}, du bist von Level {lc.OldLevel} auf Level {lc.NewLevel} abgestiegen.").ConfigureAwait(false);
			}).ConfigureAwait(false);
		}
	}
}
