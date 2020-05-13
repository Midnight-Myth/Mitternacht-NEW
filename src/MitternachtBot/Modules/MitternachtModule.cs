using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules {
	public abstract class MitternachtTopLevelModule : ModuleBase {
		protected readonly Logger      _log;
		protected          CultureInfo CultureInfo;

		public readonly string ModuleTypeName;
		public          string LowerModuleTypeName => ModuleTypeName?.ToLowerInvariant();

		public StringService  Strings      { get; set; }
		public CommandHandler CmdHandler   { get; set; }
		public ILocalization  Localization { get; set; }

		protected string Prefix => CmdHandler.GetPrefix(Context.Guild);

		protected MitternachtTopLevelModule(bool isTopLevelModule = true) {
			ModuleTypeName = isTopLevelModule ? GetType().Name : GetType().DeclaringType.Name;
			_log           = LogManager.GetCurrentClassLogger();
		}

		protected override void BeforeExecute(CommandInfo cmd) {
			CultureInfo = Localization.GetCultureInfo(Context.Guild?.Id);
		}

		protected string GetText(string key)
			=> Strings.GetText(LowerModuleTypeName, key, CultureInfo);

		protected string GetText(string key, params object[] replacements)
			=> Strings.GetText(LowerModuleTypeName, key, CultureInfo, replacements);

		protected Task<IUserMessage> MessageLocalized(string textKey, params object[] replacements) {
			var text = GetText(textKey, replacements);
			return Context.Channel.SendMessageAsync(text);
		}

		protected Task<IUserMessage> ReplyLocalized(string textKey, params object[] replacements) {
			var text = GetText(textKey, replacements);
			return Context.Channel.SendMessageAsync($"{Context.User.Mention} {text}");
		}

		protected Task<IUserMessage> ErrorLocalized(string textKey, params object[] replacements) {
			var text = GetText(textKey, replacements);
			return Context.Channel.SendErrorAsync(text);
		}

		protected Task<IUserMessage> ReplyErrorLocalized(string textKey, params object[] replacements) {
			var text = GetText(textKey, replacements);
			return Context.Channel.SendErrorAsync($"{Context.User.Mention} {text}");
		}

		protected Task<IUserMessage> ConfirmLocalized(string textKey, params object[] replacements) {
			var text = GetText(textKey, replacements);
			return Context.Channel.SendConfirmAsync(text);
		}

		protected Task<IUserMessage> ReplyConfirmLocalized(string textKey, params object[] replacements) {
			var text = GetText(textKey, replacements);
			return Context.Channel.SendConfirmAsync($"{Context.User.Mention} {text}");
		}

		protected async Task<string> GetUserInputAsync(ulong userId, ulong channelId) {
			var userInputTask = new TaskCompletionSource<string>();
			var dsc           = (DiscordSocketClient)Context.Client;
			try {
				dsc.MessageReceived += MessageReceived;

				if((await Task.WhenAny(userInputTask.Task, Task.Delay(10000))) != userInputTask.Task) {
					return null;
				}

				return await userInputTask.Task;
			} finally {
				dsc.MessageReceived -= MessageReceived;
			}

			Task MessageReceived(SocketMessage arg) {
				var _ = Task.Run(() => {
					if(!(arg is SocketUserMessage userMsg) ||
						!(userMsg.Channel is ITextChannel) ||
						userMsg.Author.Id != userId ||
						userMsg.Channel.Id != channelId) {
						return Task.CompletedTask;
					}

					if(userInputTask.TrySetResult(arg.Content)) {
						userMsg.DeleteAfter(1);
					}

					return Task.CompletedTask;
				});
				return Task.CompletedTask;
			}
		}
	}

	public abstract class MitternachtTopLevelModule<TService> : MitternachtTopLevelModule where TService : IMService {
		public TService Service { get; set; }

		protected MitternachtTopLevelModule(bool isTopLevel = true) : base(isTopLevel) { }
	}

	public abstract class MitternachtSubmodule : MitternachtTopLevelModule {
		protected MitternachtSubmodule() : base(false) { }
	}

	public abstract class MitternachtSubmodule<TService> : MitternachtTopLevelModule<TService> where TService : IMService {
		protected MitternachtSubmodule() : base(false) { }
	}
}