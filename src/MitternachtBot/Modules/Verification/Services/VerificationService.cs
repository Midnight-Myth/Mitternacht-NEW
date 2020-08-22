using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common.Collections;
using Mitternacht.Modules.Forum.Services;
using Mitternacht.Modules.Verification.Common;
using Mitternacht.Modules.Verification.Exceptions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Verification.Services {
	public class VerificationService : IMService {
		private readonly DiscordSocketClient _client;
		private readonly DbService _db;
		private readonly StringService _stringService;
		private readonly ForumService _fs;

		public event Func<IGuildUser, long, Task> UserVerified = (user, forumUserId) => Task.CompletedTask;
		public event Func<VerificationProcess, VerificationStep, Task> VerificationStep = (vp, step) => Task.CompletedTask;
		public event Func<VerificationProcess, SocketMessage, Task> VerificationMessage = (vp, msg) => Task.CompletedTask;
		//public event Func<SocketGuildUser, UserInfo, EventTrigger> UserUnverified;

		private readonly ConcurrentHashSet<VerificationProcess> VerificationProcesses = new ConcurrentHashSet<VerificationProcess>();

		public VerificationService(DiscordSocketClient client, DbService db, StringService stringService, ForumService fs) {
			_client = client;
			_db = db;
			_stringService = stringService;
			_fs = fs;
		}

		public async Task StartVerification(IGuildUser guildUser) {
			if(VerificationProcesses.Any(vp => vp.GuildUser == guildUser))
				throw new UserAlreadyVerifyingException();

			using(var uow = _db.UnitOfWork) {
				if(uow.VerifiedUsers.IsVerified(guildUser.GuildId, guildUser.Id))
					throw new UserAlreadyVerifiedException();
			}

			var verificationProcess = new VerificationProcess(guildUser, _client, _db, this, _stringService, _fs);
			try {
				await verificationProcess.Start();
			} catch(Exception) {
				verificationProcess.Dispose();
				throw;
			}
			VerificationProcesses.Add(verificationProcess);
		}

		public bool EndVerification(VerificationProcess process) {
			process.Dispose();
			return VerificationProcesses.TryRemove(process);
		}

		public IEnumerable<VerifiedUser> GetVerifiedUsers(ulong guildId) {
			using var uow = _db.UnitOfWork;
			return uow.VerifiedUsers.GetVerifiedUsers(guildId).ToList();
		}

		public int GetVerifiedUserCount(ulong guildId) {
			using var uow = _db.UnitOfWork;
			return uow.VerifiedUsers.GetNumberOfVerificationsInGuild(guildId);
		}

		public void SetVerifiedRole(ulong guildId, ulong? roleId) {
			using var uow = _db.UnitOfWork;
			uow.GuildConfigs.For(guildId).VerifiedRoleId = roleId;
			uow.SaveChanges();
		}

		public ulong? GetVerifiedRoleId(ulong guildId) {
			using var uow = _db.UnitOfWork;
			return uow.GuildConfigs.For(guildId).VerifiedRoleId;
		}

		public void SetVerifyString(ulong guildId, string verifystring) {
			using var uow = _db.UnitOfWork;
			uow.GuildConfigs.For(guildId).VerifyString = verifystring;
			uow.SaveChanges();
		}

		public string GetVerifyString(ulong guildId) {
			using var uow = _db.UnitOfWork;
			return uow.GuildConfigs.For(guildId).VerifyString;
		}

		public string GetVerificationTutorialText(ulong guildId) {
			using var uow = _db.UnitOfWork;
			return uow.GuildConfigs.For(guildId).VerificationTutorialText;
		}

		public void SetVerificationTutorialText(ulong guildId, string text) {
			using var uow = _db.UnitOfWork;
			uow.GuildConfigs.For(guildId).VerificationTutorialText = text;
			uow.SaveChanges();
		}

		public async Task AddVerifiedRoleAsync(IGuildUser guildUser) {
			using var uow = _db.UnitOfWork;

			var roleid = GetVerifiedRoleId(guildUser.GuildId);
			var role = roleid != null ? guildUser.Guild.GetRole(roleid.Value) : null;
			if(role != null)
				await guildUser.AddRoleAsync(role).ConfigureAwait(false);
		}

		public async Task SetVerified(IGuildUser guildUser, long forumUserId) {
			using var uow = _db.UnitOfWork;
			if(!uow.VerifiedUsers.SetVerified(guildUser.GuildId, guildUser.Id, forumUserId))
				throw new UserCannotVerifyException();

			await AddVerifiedRoleAsync(guildUser).ConfigureAwait(false);

			await UserVerified.Invoke(guildUser, forumUserId).ConfigureAwait(false);
		}

		public string[] GetAdditionalVerificationUsers(ulong guildId) {
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildId);
			return string.IsNullOrWhiteSpace(gc.AdditionalVerificationUsers) ? new string[0] : gc.AdditionalVerificationUsers.Split(',');
		}

		public string[] GetVerificationConversationUsers(ulong guildId)
			=> GetAdditionalVerificationUsers(guildId).Prepend(_fs.Forum.SelfUser.Username).ToArray();

		public void SetAdditionalVerificationUsers(ulong guildId, string[] users) {
			using var uow = _db.UnitOfWork;
			var gc = uow.GuildConfigs.For(guildId);
			gc.AdditionalVerificationUsers = string.Join(',', users);
			uow.GuildConfigs.Update(gc);
			uow.SaveChanges();
		}

		public async Task InvokeVerificationStep(VerificationProcess process, VerificationStep step)
			=> await VerificationStep.Invoke(process, step).ConfigureAwait(false);

		public async Task InvokeVerificationMessage(VerificationProcess process, SocketMessage msg)
			=> await VerificationMessage.Invoke(process, msg).ConfigureAwait(false);
	}
}