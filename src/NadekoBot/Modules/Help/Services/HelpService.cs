using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Help.Services
{
    public class HelpService : ILateExecutor, INService
    {
        private readonly IBotConfigProvider _bc;
        private readonly CommandHandler _ch;
        private readonly StringService _strings;

        public HelpService(IBotConfigProvider bc, CommandHandler ch, StringService strings)
        {
            _bc = bc;
            _ch = ch;
            _strings = strings;
        }

        public async Task LateExecute(DiscordSocketClient client, IGuild guild, IUserMessage msg)
        {
            try
            {
                if(guild == null)
                    await msg.Channel.SendMessageAsync(_bc.BotConfig.DMHelpString).ConfigureAwait(false);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public EmbedBuilder GetCommandHelp(CommandInfo com, IGuild guild)
        {
            var prefix = _ch.GetPrefix(guild);

            var str = Format.Bold(string.Join("/", com.Aliases.Select(alias => $" `{prefix}{alias}` ").ToArray()).Trim());
            return new EmbedBuilder()
                .AddField(str, $"{com.RealSummary(prefix)} {GetCommandRequirements(com, guild)}", true)
                .AddField(GetText("usage", guild), com.RealRemarks(prefix), true)
                .WithFooter(GetText("module", guild, $"{com.Module.GetTopLevelModule().Name}{(com.Module == com.Module.GetTopLevelModule() ? "" : $" ({com.Module.Name})")}"))
                .WithOkColor();
        }

        //todo: add OwnerOrGuildPermissionAttribute
        public string GetCommandRequirements(CommandInfo cmd, IGuild guild) =>
            string.Join(" ", cmd.Preconditions
                  .Where(ca => ca is OwnerOnlyAttribute || ca is RequireUserPermissionAttribute /*|| ca is OwnerOrGuildPermissionAttribute*/)
                  .Select(ca =>
                  {
                      if (ca is OwnerOnlyAttribute)
                          return Format.Bold(GetText("bot_owner_only", guild));
                      var cau = (RequireUserPermissionAttribute)ca;
                      if (cau.GuildPermission != null)
                          return Format.Bold(GetText("server_permission", guild, cau.GuildPermission))
                                       .Replace("Guild", "Server");
                      return Format.Bold(GetText("channel_permission", guild, cau.ChannelPermission))
                                       .Replace("Guild", "Server");
                  }));

        private string GetText(string text, IGuild guild, params object[] replacements) =>
            _strings.GetText(text, guild?.Id, "help", replacements);
    }
}
