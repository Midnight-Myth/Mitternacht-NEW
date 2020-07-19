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
			=> Strings.GetText(ModuleTypeName, key, CultureInfo);

		protected string GetText(string key, params object[] replacements)
			=> Strings.GetText(ModuleTypeName, key, CultureInfo, replacements);

		protected Task<IUserMessage> MessageLocalized(string textKey, params object[] replacements)
			=> Context.Channel.SendMessageAsync(GetText(textKey, replacements));

		protected Task<IUserMessage> ReplyLocalized(string textKey, params object[] replacements)
			=> Context.Channel.SendMessageAsync($"{Context.User.Mention} {GetText(textKey, replacements)}");

		protected Task<IUserMessage> ErrorLocalized(string textKey, params object[] replacements)
			=> Context.Channel.SendErrorAsync(GetText(textKey, replacements));

		protected Task<IUserMessage> ReplyErrorLocalized(string textKey, params object[] replacements)
			=> Context.Channel.SendErrorAsync($"{Context.User.Mention} {GetText(textKey, replacements)}");

		protected Task<IUserMessage> ConfirmLocalized(string textKey, params object[] replacements)
			=> Context.Channel.SendConfirmAsync(GetText(textKey, replacements));

		protected Task<IUserMessage> ReplyConfirmLocalized(string textKey, params object[] replacements)
			=> Context.Channel.SendConfirmAsync($"{Context.User.Mention} {GetText(textKey, replacements)}");

		protected async Task<string> GetUserInputAsync(ulong userId, ulong channelId) {
			var userInputTask = new TaskCompletionSource<string>();
			var dsc           = (DiscordSocketClient)Context.Client;

			Task MessageReceived(SocketMessage arg) {
				var _ = Task.Run(() => {
					if(arg is SocketUserMessage userMsg && userMsg.Channel is ITextChannel && userMsg.Author.Id == userId && userMsg.Channel.Id == channelId && userInputTask.TrySetResult(arg.Content)) {
						userMsg.DeleteAfter(1);
					}
				});

				return Task.CompletedTask;
			}

			try {
				dsc.MessageReceived += MessageReceived;

				return await Task.WhenAny(userInputTask.Task, Task.Delay(10000)) != userInputTask.Task ? null : await userInputTask.Task;
			} finally {
				dsc.MessageReceived -= MessageReceived;
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