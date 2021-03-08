using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using GommeHDnetForumAPI.Models.Entities;
using GommeHDnetForumAPI.Exceptions;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Forum.Services;
using Mitternacht.Database;
using System;

namespace Mitternacht.Modules.Forum {
	public partial class Forum : MitternachtTopLevelModule<ForumService> {
		private readonly IUnitOfWork uow;

		public Forum(IUnitOfWork uow) {
			this.uow = uow;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		public async Task ReinitForum() {
			Service.InitForumInstance();
			await ConfirmLocalized("reinit_forum").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		public async Task UserInfoForum(IGuildUser user = null) {
			user ??= (IGuildUser)Context.User;

			UserInfo uinfo = null;
			var forumId = uow.VerifiedUsers.GetVerifiedUser(Context.Guild.Id, user.Id)?.ForumUserId;
			if(forumId != null) {
				try {
					uinfo = await Service.Forum.GetUserInfo(forumId.Value).ConfigureAwait(false);
				} catch(UserNotFoundException) {
					await ReplyErrorLocalized("forum_user_not_existing", user.ToString()).ConfigureAwait(false);
					return;
				} catch(UserProfileAccessException) {
					await ReplyErrorLocalized("forum_user_not_seeable", user.ToString()).ConfigureAwait(false);
					return;
				} catch(Exception e) {
					_log.Error(e);
				}
			}

			if(uinfo != null) {
				var embed = ForumUserInfoBuilder(uinfo).WithTitle(GetText("forumuserinfo_title", user.ToString()));

				await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
			} else {
				await ReplyErrorLocalized("forum_user_not_accessible", user.ToString()).ConfigureAwait(false);
			}
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[Priority(0)]
		[RequireContext(ContextType.Guild)]
		public async Task ForumUserInfo(string username)
			=> await PrivateForumUserInfoHandler(username).ConfigureAwait(false);

		[MitternachtCommand, Usage, Description, Aliases]
		[Priority(1)]
		[RequireContext(ContextType.Guild)]
		public async Task ForumUserInfo(long userId)
			=> await PrivateForumUserInfoHandler(userId: userId).ConfigureAwait(false);

		private async Task PrivateForumUserInfoHandler(string username = null, long? userId = null) {
			if(username != null || userId.HasValue) {
				var      userText = userId != null ? userId.Value.ToString() : username;
				UserInfo uinfo    = null;
				try {
					uinfo = userId.HasValue ? await Service.Forum.GetUserInfo(userId.Value).ConfigureAwait(false) : await Service.Forum.GetUserInfo(username).ConfigureAwait(false);
				} catch(UserNotFoundException) {
					await ReplyErrorLocalized("forum_user_not_existing", userText).ConfigureAwait(false);
					return;
				} catch(UserProfileAccessException) {
					await ReplyErrorLocalized("forum_user_not_seeable", userText).ConfigureAwait(false);
					return;
				} catch(Exception e) {
					_log.Error(e);
				}

				if(uinfo != null) {
					var embed = ForumUserInfoBuilder(uinfo);
					var verifiedUserId = uow.VerifiedUsers.GetVerifiedUser(Context.Guild.Id, uinfo.Id)?.UserId;
					if(verifiedUserId != null) {
						var verifiedUser = await Context.Guild.GetUserAsync(verifiedUserId.Value);
						embed.WithTitle(GetText("forumuserinfo_title", verifiedUser?.ToString() ?? verifiedUserId.ToString()));
					}

					await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
				} else {
					await ReplyErrorLocalized("forum_user_not_accessible", userText).ConfigureAwait(false);
				}
			} else {
				await ReplyErrorLocalized("dev_failed").ConfigureAwait(false);
			}
		}

		private EmbedBuilder ForumUserInfoBuilder(UserInfo uinfo) {
			var embed = new EmbedBuilder()
						.WithOkColor()
						.WithThumbnailUrl(uinfo.AvatarUrl)
						.AddField(GetText("name"), $"[{uinfo.Username}]({uinfo.Url})", true)
						.AddField(GetText("id"), uinfo.Id, true)
						.AddField(GetText("gender"), uinfo.Gender.ToString(), true);

			if(!string.IsNullOrWhiteSpace(uinfo.Status))
				embed.AddField(GetText("status"), uinfo.Status, true);
			if(uinfo.PostCount != null)
				embed.AddField(GetText("posts"), uinfo.PostCount.Value, true);
			if(uinfo.LikeCount != null)
				embed.AddField(GetText("likes"), uinfo.LikeCount.Value, true);
			if(uinfo.Trophies != null)
				embed.AddField(GetText("trophies"), uinfo.Trophies.Value, true);
			if(!string.IsNullOrWhiteSpace(uinfo.Location))
				embed.AddField(GetText("location"), uinfo.Location, true);
			if(!string.IsNullOrWhiteSpace(uinfo.UserTitle))
				embed.AddField(GetText("rank"), uinfo.UserTitle, true);

			return embed;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task ForumInfo() {
			if(Service.LoggedIn) {
				await ConfirmLocalized("foruminfo_logged_in", $"[{Service.Forum.SelfUser.Username}]({Service.Forum.SelfUser.UrlPath})").ConfigureAwait(false);
			} else if(Service.HasForumInstance) {
				await ConfirmLocalized("foruminfo_instance").ConfigureAwait(false);
			} else {
				await ConfirmLocalized("foruminfo_no_instance").ConfigureAwait(false);
			}
		}
	}
}