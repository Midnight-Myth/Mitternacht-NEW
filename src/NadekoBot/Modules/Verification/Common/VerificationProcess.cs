using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GommeHDnetForumAPI.DataModels.Entities;
using GommeHDnetForumAPI.Exceptions;
using Mitternacht.Extensions;
using Mitternacht.Modules.Forum.Services;
using Mitternacht.Modules.Verification.Exceptions;
using Mitternacht.Modules.Verification.Services;
using Mitternacht.Services;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Verification.Common {
	public class VerificationProcess : IDisposable {
		public IGuildUser GuildUser { get; }
		public readonly DateTime ProcessStartTime = DateTime.Now;

		private IMessageChannel UserChannel;
		private long ForumUserId;

		private readonly DiscordSocketClient _client;
		private readonly DbService _db;
		private readonly VerificationService _verificationService;
		private readonly StringService _stringService;
		private readonly ForumService _fs;

		private const string AbortString = "Abbruch";

		public VerificationProcess(IGuildUser guildUser, DiscordSocketClient client, DbService db, VerificationService verificationService, StringService stringService, ForumService fs) {
			GuildUser = guildUser;
			_client = client;
			_db = db;
			_verificationService = verificationService;
			_stringService = stringService;
			_fs = fs;
		}

		public async Task Start() {
			UserChannel = await GuildUser.GetOrCreateDMChannelAsync();

			//verification process intro
			await EmbedAsync("welcome_to_verification");
			await EmbedAsync("why_do_we_need_verification");

			//Step 1 - forum name input
			await EmbedAsync("write_your_forumname");

			_client.MessageReceived += ReceiveAbort;
			_client.MessageReceived += Step1_ReceiveForumName;
		}

		public void Stop() {
			_client.MessageReceived -= Step1_ReceiveForumName;
			_client.MessageReceived -= Step2_ReadPrivateForumMessage;
			_client.MessageReceived -= Step3_ReadDiscordBotkey;
			_client.MessageReceived -= ReceiveAbort;
		}

		public void Dispose()
			=> Stop();

		private async Task ReceiveAbort(SocketMessage msg) {
			if(msg.Channel == UserChannel) {
				if(msg.Content.Equals(AbortString, StringComparison.OrdinalIgnoreCase)) {
					Stop();

					await ConfirmAsync("process_aborted");
					_verificationService.EndVerification(this);
				}
			}
		}

		private async Task Step1_ReceiveForumName(SocketMessage msg) {
			if(_fs.LoggedIn) {
				if(msg.Channel == UserChannel) {
					var forumname = msg.Content.Trim();
					UserInfo forumUser;

					try {
						forumUser = long.TryParse(forumname, out var forumUserId) ? await _fs.Forum.GetUserInfo(forumUserId) : await _fs.Forum.GetUserInfo(forumname);
					} catch(UserNotFoundException) {
						await ErrorAsync("forumaccount_not_found_try_again", forumname);
						return;
					} catch(UserProfileAccessException) {
						await ErrorAsync("forumaccount_not_viewable_try_again");
						return;
					}

					if(!forumUser.Verified.Value) {
						await ErrorAsync("forumaccount_not_verified_try_again");
						return;
					}

					ulong? passwordChannelId;
					using(var uow = _db.UnitOfWork) {
						if(uow.VerifiedUsers.IsForumUserVerified(GuildUser.GuildId, forumUser.Id)) {
							await ErrorAsync("already_verified_try_again");
							return;
						}

						passwordChannelId = uow.GuildConfigs.For(GuildUser.GuildId).VerificationPasswordChannelId;
					}

					ForumUserId = forumUser.Id;

					//prepare Step 2
					var verificationKey = VerificationKeyManager.GenerateVerificationKey(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Forum);
					var conversationUrl = _fs.Forum.GetConversationCreationUrl(_verificationService.GetVerificationConversationUsers(GuildUser.GuildId));
					var passwordChannel = passwordChannelId.HasValue ? await GuildUser.Guild.GetTextChannelAsync(passwordChannelId.Value) : null;
					var passwordChannelString = passwordChannel?.Mention ?? "";

					//Step 2 - Send a private message in the GommeHDnet forum
					await EmbedAsync("send_message_in_forum", verificationKey.Key, GuildUser.ToString(), conversationUrl, passwordChannelString, passwordChannelString);

					_client.MessageReceived -= Step1_ReceiveForumName;
					_client.MessageReceived += Step2_ReadPrivateForumMessage;
				}
			} else {
				await ErrorAsync("forum_not_logged_in");
			}
		}

		private async Task Step2_ReadPrivateForumMessage(SocketMessage msg) {
			if(_fs.LoggedIn) {
				if(msg.Channel == UserChannel) {
					var conversations = await _fs.Forum.GetConversations(startPage: 0, pageCount: 2);
					var conversation = conversations.FirstOrDefault(c => c.Author.Id == ForumUserId);

					if(conversation is null) {
						await ErrorAsync("no_message_by_author");
						return;
					}
					await conversation.DownloadMessagesAsync();
					using(var uow = _db.UnitOfWork) {
						var verifystring = uow.GuildConfigs.For(GuildUser.GuildId).VerifyString;
						if(!string.IsNullOrWhiteSpace(verifystring) && !conversation.Title.Equals(verifystring, StringComparison.OrdinalIgnoreCase)) {
							await ErrorAsync("message_wrong_title_try_again");
							return;
						}
					}
					var message = conversation.Messages.First().Content;

					var verificationKey = VerificationKeyManager.GetKeyString(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Forum);
					if(!message.Contains(verificationKey)) {
						await ErrorAsync("message_no_botkey_try_again");
						return;
					}
					if(message.Contains(GuildUser.ToString()) || message.Contains(GuildUser.Id.ToString())) {
						await ErrorAsync("message_no_discorduser_try_again");
						return;
					}

					//prepare Step 3
					var key = VerificationKeyManager.GenerateVerificationKey(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Discord);
					var success = await conversation.Reply(key.Key);
					if(!success) {
						await ErrorAsync("conversation_answer_failed_try_again");
						VerificationKeyManager.RemoveKey(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Discord);
						return;
					}

					//complete Step 2
					//this has to be below Step 3 preparation to allow handling the failure of sending the second verification key.
					VerificationKeyManager.RemoveKey(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Forum);

					await EmbedAsync("send_botkey_in_dm");

					_client.MessageReceived -= Step2_ReadPrivateForumMessage;
					_client.MessageReceived += Step3_ReadDiscordBotkey;
				}
			} else {
				await ErrorAsync("forum_not_logged_in");
			}
		}

		private async Task Step3_ReadDiscordBotkey(SocketMessage msg) {
			if(msg.Channel == UserChannel) {
				var keyString = VerificationKeyManager.GetKeyString(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Discord);
				if(!msg.Content.Equals(keyString)) {
					await ErrorAsync("dm_no_botkey");
					return;
				}

				try {
					await _verificationService.SetVerified(GuildUser, ForumUserId);
					await EmbedAsync("process_completed");
				} catch(UserCannotVerifyException) {
					await ErrorAsync("user_cannot_verify");
				}
			}
		}


		private string GetText(string key, params object[] replacements)
			=> _stringService.GetText(key, GuildUser.GuildId, "verification", replacements);

		private async Task<IUserMessage> EmbedAsync(string key, params object[] replacements)
			=> await UserChannel.SendConfirmAsync(GetText($"{key}_title"), GetText($"{key}_text", replacements)).ConfigureAwait(false);

		private async Task<IUserMessage> ConfirmAsync(string key, params object[] replacements)
			=> await UserChannel.SendConfirmAsync(GetText(key, replacements)).ConfigureAwait(false);

		private async Task<IUserMessage> ErrorAsync(string key, params object[] replacements)
			=> await UserChannel.SendErrorAsync(GetText(key, replacements)).ConfigureAwait(false);
	}
}
