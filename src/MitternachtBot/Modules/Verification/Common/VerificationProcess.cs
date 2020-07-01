using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GommeHDnetForumAPI.Models.Entities;
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
			await _verificationService.InvokeVerificationStep(this, VerificationStep.Started).ConfigureAwait(false);
			UserChannel = await GuildUser.GetOrCreateDMChannelAsync().ConfigureAwait(false);

			var eb = new EmbedBuilder().WithOkColor();
			//verification process intro
			eb.AddField(GetText("welcome_to_verification_title"), GetText("welcome_to_verification_text", AbortString));
			eb.AddField(GetText("why_do_we_need_verification_title"), GetText("why_do_we_need_verification_text"));

			//Step 1 - forum name input
			eb.AddField(GetText("write_your_forumname_title"), GetText("write_your_forumname_text"));

			await UserChannel.EmbedAsync(eb).ConfigureAwait(false);

			_client.MessageReceived += Step1_ReceiveForumName;
		}

		public void Stop() {
			_client.MessageReceived -= Step1_ReceiveForumName;
			_client.MessageReceived -= Step2_ReadPrivateForumMessage;
			_client.MessageReceived -= Step3_ReadDiscordBotkey;
		}

		public void Dispose()
			=> Stop();

		private async Task<bool> ReceiveAbort(SocketMessage msg) {
			if(msg.Content.Equals(AbortString, StringComparison.OrdinalIgnoreCase)) {
				Stop();

				await ConfirmAsync("process_aborted").ConfigureAwait(false);
				_verificationService.EndVerification(this);

				await _verificationService.InvokeVerificationStep(this, VerificationStep.Aborted).ConfigureAwait(false);
				return true;
			}
			return false;
		}

		private async Task Step1_ReceiveForumName(SocketMessage msg) {
			if(msg.Channel.Id == UserChannel.Id && msg.Author.Id == GuildUser.Id) {
				await _verificationService.InvokeVerificationMessage(this, msg).ConfigureAwait(false);

				if(!await ReceiveAbort(msg)) {
					if(_fs.LoggedIn) {
						var forumname = msg.Content.Trim();
						UserInfo forumUser;

						try {
							try {
								forumUser = await _fs.Forum.GetUserInfo(forumname).ConfigureAwait(false);
							} catch(Exception) {
								if(long.TryParse(forumname, out var forumUserId)) {
									forumUser = await _fs.Forum.GetUserInfo(forumUserId).ConfigureAwait(false);
								} else {
									throw;
								}
							}
						} catch(UserNotFoundException) {
							await ErrorAsync("forumaccount_not_found_try_again", forumname).ConfigureAwait(false);
							return;
						} catch(UserProfileAccessException) {
							await ErrorAsync("forumaccount_not_viewable_try_again").ConfigureAwait(false);
							return;
						}

						if(forumUser.Id == _fs.Forum.SelfUser.Id) {
							await ErrorAsync("forumaccount_is_self_try_again").ConfigureAwait(false);
							return;
						}

						if(!forumUser.Verified.Value) {
							await ErrorAsync("forumaccount_not_verified_try_again").ConfigureAwait(false);
							return;
						}

						using var uow = _db.UnitOfWork;
						if(uow.VerifiedUsers.IsForumUserVerified(GuildUser.GuildId, forumUser.Id)) {
							await ErrorAsync("forumaccount_already_verified_try_again").ConfigureAwait(false);
							return;
						}

						var passwordChannelId = uow.GuildConfigs.For(GuildUser.GuildId).VerificationPasswordChannelId;

						ForumUserId = forumUser.Id;

						await _verificationService.InvokeVerificationStep(this, VerificationStep.ForumNameSent).ConfigureAwait(false);

						//prepare Step 2
						var verificationKey = VerificationKeyManager.GenerateVerificationKey(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Forum);
						var conversationUrl = _fs.Forum.GetConversationCreationUrl(_verificationService.GetVerificationConversationUsers(GuildUser.GuildId));
						var passwordChannel = passwordChannelId.HasValue ? await GuildUser.Guild.GetTextChannelAsync(passwordChannelId.Value).ConfigureAwait(false) : null;
						var passwordChannelString = passwordChannel?.Mention ?? "";

						//Step 2 - Send a private message in the GommeHDnet forum
						await EmbedAsync("send_message_in_forum", verificationKey.Key, GuildUser.ToString(), conversationUrl, passwordChannelString, passwordChannelString).ConfigureAwait(false);

						_client.MessageReceived -= Step1_ReceiveForumName;
						_client.MessageReceived += Step2_ReadPrivateForumMessage;
					} else {
						await ErrorAsync("forum_not_logged_in").ConfigureAwait(false);
					}
				}
			}
		}

		private async Task Step2_ReadPrivateForumMessage(SocketMessage msg) {
			if(msg.Channel.Id == UserChannel.Id && msg.Author.Id == GuildUser.Id) {
				await _verificationService.InvokeVerificationMessage(this, msg).ConfigureAwait(false);

				if(!await ReceiveAbort(msg).ConfigureAwait(false)) {
					if(_fs.LoggedIn) {
						var conversations = await _fs.Forum.GetConversations(startPage: 1, pageCount: 2).ConfigureAwait(false);
						var conversation = conversations.Where(c => c.Author is UserInfo).FirstOrDefault(c => (c.Author as UserInfo).Id == ForumUserId);

						if(conversation is null) {
							await ErrorAsync("no_message_by_author");
							return;
						}
						await conversation.DownloadMessagesAsync().ConfigureAwait(false);
						using var uow = _db.UnitOfWork;
						var verifystring = uow.GuildConfigs.For(GuildUser.GuildId).VerifyString;
						if(!string.IsNullOrWhiteSpace(verifystring) && !conversation.Title.Contains(verifystring, StringComparison.OrdinalIgnoreCase)) {
							await ErrorAsync("message_wrong_title_try_again").ConfigureAwait(false);
							return;
						}
						var message = conversation.Messages.First().Content;

						var verificationKey = VerificationKeyManager.GetKeyString(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Forum);
						if(!message.Contains(verificationKey)) {
							await ErrorAsync("message_no_botkey_try_again").ConfigureAwait(false);
							return;
						}
						if(!message.Contains(GuildUser.ToString()) && !message.Contains(GuildUser.Id.ToString())) {
							await ErrorAsync("message_no_discorduser_try_again").ConfigureAwait(false);
							return;
						}

						//prepare Step 3
						var key = VerificationKeyManager.GenerateVerificationKey(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Discord);
						var success = await conversation.Reply(key.Key).ConfigureAwait(false);
						if(!success) {
							await ErrorAsync("conversation_answer_failed_try_again").ConfigureAwait(false);
							VerificationKeyManager.RemoveKey(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Discord);
							return;
						}

						await _verificationService.InvokeVerificationStep(this, VerificationStep.ForumConversationCreated).ConfigureAwait(false);

						//complete Step 2
						//this has to be below Step 3 preparation to allow handling the failure of sending the second verification key.
						VerificationKeyManager.RemoveKey(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Forum);

						await EmbedAsync("send_botkey_in_dm").ConfigureAwait(false);

						_client.MessageReceived -= Step2_ReadPrivateForumMessage;
						_client.MessageReceived += Step3_ReadDiscordBotkey;
					} else {
						await ErrorAsync("forum_not_logged_in").ConfigureAwait(false);
					}
				}
			}
		}

		private async Task Step3_ReadDiscordBotkey(SocketMessage msg) {
			if(msg.Channel.Id == UserChannel.Id && msg.Author.Id == GuildUser.Id) {
				await _verificationService.InvokeVerificationMessage(this, msg).ConfigureAwait(false);

				if(!await ReceiveAbort(msg)) {
					var keyString = VerificationKeyManager.GetKeyString(GuildUser.GuildId, GuildUser.Id, ForumUserId, VerificationKeyScope.Discord);
					if(!msg.Content.Equals(keyString)) {
						await ErrorAsync("dm_no_botkey").ConfigureAwait(false);
						return;
					}

					try {
						await _verificationService.SetVerified(GuildUser, ForumUserId);
						await EmbedAsync("process_completed");
						_verificationService.EndVerification(this);

						await _verificationService.InvokeVerificationStep(this, VerificationStep.Ended).ConfigureAwait(false);
					} catch(UserCannotVerifyException) {
						await ErrorAsync("user_cannot_verify").ConfigureAwait(false);
					}
				}
			}
		}


		private string GetText(string key, params object[] replacements)
			=> _stringService.GetText("verification", key, GuildUser.GuildId, replacements);

		private async Task<IUserMessage> EmbedAsync(string key, params object[] replacements)
			=> await UserChannel.SendConfirmAsync(GetText($"{key}_text", replacements), GetText($"{key}_title")).ConfigureAwait(false);

		private async Task<IUserMessage> ConfirmAsync(string key, params object[] replacements)
			=> await UserChannel.SendConfirmAsync(GetText(key, replacements)).ConfigureAwait(false);

		private async Task<IUserMessage> ErrorAsync(string key, params object[] replacements)
			=> await UserChannel.SendErrorAsync(GetText(key, replacements)).ConfigureAwait(false);
	}
}
