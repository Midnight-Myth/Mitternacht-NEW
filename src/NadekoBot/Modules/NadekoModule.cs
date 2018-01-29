using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules
{
    public abstract class MitternachtTopLevelModule : ModuleBase
    {
        protected readonly Logger _log;
        protected CultureInfo CultureInfo;

        public readonly string ModuleTypeName;
        public string LowerModuleTypeName => ModuleTypeName?.ToLowerInvariant();

        public NadekoStrings Strings { get; set; }
        public CommandHandler CmdHandler { get; set; }
        public ILocalization Localization { get; set; }

        public string Prefix => CmdHandler.GetPrefix(Context.Guild);

        protected MitternachtTopLevelModule(bool isTopLevelModule = true)
        {
            //if it's top level module
            ModuleTypeName = isTopLevelModule ? GetType().Name : GetType().DeclaringType.Name;
            _log = LogManager.GetCurrentClassLogger();
        }

        protected override void BeforeExecute(CommandInfo cmd)
        {
            CultureInfo = Localization.GetCultureInfo(Context.Guild?.Id);
        }

        //public Task<IUserMessage> ReplyConfirmLocalized(string titleKey, string textKey, string url = null, string footer = null)
        //{
        //    var title = MitternachtBot.ResponsesResourceManager.GetString(titleKey, cultureInfo);
        //    var text = MitternachtBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendConfirmAsync(title, text, url, footer);
        //}

        //public Task<IUserMessage> ReplyConfirmLocalized(string textKey)
        //{
        //    var text = MitternachtBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendConfirmAsync(Context.User.Mention + " " + textKey);
        //}

        //public Task<IUserMessage> ReplyErrorLocalized(string titleKey, string textKey, string url = null, string footer = null)
        //{
        //    var title = MitternachtBot.ResponsesResourceManager.GetString(titleKey, cultureInfo);
        //    var text = MitternachtBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendErrorAsync(title, text, url, footer);
        //}

        protected string GetText(string key) =>
            Strings.GetText(key, CultureInfo, LowerModuleTypeName);

        protected string GetText(string key, params object[] replacements) =>
            Strings.GetText(key, CultureInfo, LowerModuleTypeName, replacements);

        public Task<IUserMessage> ErrorLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendErrorAsync(text);
        }

        public Task<IUserMessage> ReplyErrorLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendErrorAsync(Context.User.Mention + " " + text);
        }

        public Task<IUserMessage> ConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendConfirmAsync(text);
        }

        public Task<IUserMessage> ReplyConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendConfirmAsync(Context.User.Mention + " " + text);
        }
        
        // TypeConverter typeConverter = TypeDescriptor.GetConverter(propType); ?
        public async Task<string> GetUserInputAsync(ulong userId, ulong channelId)
        {
            var userInputTask = new TaskCompletionSource<string>();
            var dsc = (DiscordSocketClient)Context.Client;
            try
            {
                dsc.MessageReceived += MessageReceived;

                if ((await Task.WhenAny(userInputTask.Task, Task.Delay(10000))) != userInputTask.Task)
                {
                    return null;
                }

                return await userInputTask.Task;
            }
            finally
            {
                dsc.MessageReceived -= MessageReceived;
            }

            Task MessageReceived(SocketMessage arg)
            {
                var _ = Task.Run(() =>
                {
                    if (!(arg is SocketUserMessage userMsg) ||
                        !(userMsg.Channel is ITextChannel) ||
                        userMsg.Author.Id != userId ||
                        userMsg.Channel.Id != channelId)
                    {
                        return Task.CompletedTask;
                    }

                    if (userInputTask.TrySetResult(arg.Content))
                    {
                        userMsg.DeleteAfter(1);
                    }
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            }
        }
    }
    
    public abstract class MitternachtTopLevelModule<TService> : MitternachtTopLevelModule where TService : INService
    {
        public TService Service { get; set; }

        protected MitternachtTopLevelModule(bool isTopLevel = true) : base(isTopLevel)
        {
        }
    }

    public abstract class MitternachtSubmodule : MitternachtTopLevelModule
    {
        protected MitternachtSubmodule() : base(false) { }
    }

    public abstract class MitternachtSubmodule<TService> : MitternachtTopLevelModule<TService> where TService : INService
    {
        protected MitternachtSubmodule() : base(false)
        {
        }
    }
}