using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Help.Services;
using Mitternacht.Modules.Permissions.Services;
using Mitternacht.Resources;
using Mitternacht.Services;
using YamlDotNet.Serialization;

namespace Mitternacht.Modules.Help {
	public partial class Help : MitternachtTopLevelModule<HelpService> {
		public const string PatreonUrl = "https://patreon.com/plauderkonfi";

		private readonly IBotCredentials         _creds;
		private readonly IBotConfigProvider      _config;
		private readonly CommandService          _cmds;
		private readonly GlobalPermissionService _perms;

		public string HelpString => string.Format(_config.BotConfig.HelpString, _creds.ClientId, Prefix);

		public Help(IBotCredentials creds, GlobalPermissionService perms, IBotConfigProvider config, CommandService cmds) {
			_creds = creds;
			_config = config;
			_cmds = cmds;
			_perms = perms;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task Modules() {
			var embed = new EmbedBuilder().WithOkColor()
				.WithFooter(efb => efb.WithText(GetText("modules_footer", Prefix)))
				.WithTitle(GetText("list_of_modules"))
				.WithDescription(string.Join("\n", _cmds.Modules.GroupBy(m => m.GetTopLevelModule())
										 .Where(m => !_perms.BlockedModules.Contains(m.Key.Name.ToLowerInvariant()))
										 .Select(m => $"• {m.Key.Name}")
										 .OrderBy(s => s)));
			await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task Submodules() {
			var embed = new EmbedBuilder()
				.WithOkColor()
				.WithFooter(GetText("modules_footer", Prefix))
				.WithTitle(GetText("list_of_submodules"))
				.WithDescription(string.Join("\n", _cmds.Modules.GroupBy(m => m.GetTopLevelModule())
					.Where(m => !_perms.BlockedModules.Any(bm => bm.Equals(m.Key.Name, StringComparison.OrdinalIgnoreCase)))
					.OrderBy(m => m.Key.Name)
					.Select(m => {
						var s = $"{m.Key.Name}";
						var sms = m.Where(sm => sm.IsSubmodule).ToList();
						if(sms.Any())
							s += "\n" + string.Join("\n", sms.Select(sm => $"â€¢ {sm.GetModuleName()}"));
						return s;
					})));
			await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task Commands([Remainder] string module = null) {
			var channel = Context.Channel;

			module = module?.Trim();
			if(string.IsNullOrWhiteSpace(module))
				return;
			var cmds = _cmds.Commands
				.Where(c => c.Module.GetModuleName().StartsWith(module, StringComparison.OrdinalIgnoreCase)
							|| c.Module.GetTopLevelModule().GetModuleName().StartsWith(module, StringComparison.OrdinalIgnoreCase))
				.Where(c => !_perms.BlockedCommands.Any(bc => bc.Equals(c.Aliases.First(), StringComparison.OrdinalIgnoreCase)))
				.Distinct(new CommandTextEqualityComparer())
				.GroupBy(c => c.Module)
				.OrderBy(g => g.Key.IsSubmodule ? g.Key.Name : "0", new ModuleOrderComparer())
				.ToArray();

			if(!cmds.Any()) {
				await ReplyErrorLocalized("module_not_found").ConfigureAwait(false);
				return;
			}
			var j = 0;
			var groups = cmds.GroupBy(x => (j+=x.Count()) / 48).ToArray();

			for(var i = 0; i < groups.Length; i++) {
				var text = $"{(i == 0 ? $"📃 **{GetText("list_of_commands")}**\n" : "")}```css\n";
				text += string.Join("\n", groups[i].Select(sm => {
					var o = 0;
					return $"{sm.Key.GetModuleName()}\n{string.Join("\n", sm.GroupBy(c => o++ / 3).Select(col => string.Concat(col.Select(c => $"{Prefix + c.Aliases.First(),-16} {$"[{c.Aliases.Skip(1).FirstOrDefault()}]",-9}"))))}";
				}));

				text += "```";
				await channel.SendMessageAsync(text).ConfigureAwait(false);
			}

			await ConfirmLocalized("commands_instr", Prefix).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[Priority(0)]
		public async Task HelpCommand([Remainder] string fail) {
			await ReplyErrorLocalized("command_not_found").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[Priority(1)]
		public async Task HelpCommand([Remainder] CommandInfo com = null) {
			var channel = Context.Channel;

			if(com == null) {
				var ch = channel is ITextChannel ? await ((IGuildUser)Context.User).CreateDMChannelAsync() : channel;
				await ch.SendMessageAsync(HelpString).ConfigureAwait(false);
				return;
			}

			var embed = Service.GetCommandHelp(com, Context.Guild);
			await channel.EmbedAsync(embed).ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[RequireContext(ContextType.Guild)]
		[OwnerOnly]
		public async Task Hgit() {
			var helpstr = new StringBuilder();
			helpstr.AppendLine($"## {GetText("table_of_contents")}");
			helpstr.AppendLine(string.Join("\n", _cmds.Modules.Where(m => !string.Equals(m.GetTopLevelModule().Name, "help", StringComparison.OrdinalIgnoreCase))
				.Select(m => m.GetTopLevelModule().Name)
				.Distinct()
				.OrderBy(m => m)
				.Prepend("Help")
				.Select(m => string.Format("- [{0}](#{1})", m, m.ToLowerInvariant()))));
			helpstr.AppendLine();
			string lastModule = null;
			foreach(var com in _cmds.Commands.OrderBy(com => com.Module.GetTopLevelModule().Name).GroupBy(c => c.Aliases.First()).Select(g => g.First())) {
				var module = com.Module.GetTopLevelModule();
				if(module.Name != lastModule) {
					if(lastModule != null) {
						helpstr.AppendLine();
						helpstr.AppendLine($"###### [{GetText("back_to_toc")}](#{GetText("table_of_contents").ToLowerInvariant().Replace(' ', '-')})");
					}
					helpstr.AppendLine();
					helpstr.AppendLine("### " + module.Name + "  ");
					helpstr.AppendLine($"{GetText("cmd_and_alias")} | {GetText("desc")} | {GetText("usage")}");
					helpstr.AppendLine("----------------|--------------|-------");
					lastModule = module.Name;
				}
				helpstr.AppendLine($"{string.Join(" ", com.Aliases.Select(a => "`" + Prefix + a + "`"))} |" +
								   $" {string.Format(com.Summary, Prefix)} {Service.GetCommandRequirements(com, Context.Guild)} |" +
								   $" {string.Format(com.Remarks, Prefix)}");
			}

			Directory.CreateDirectory("./docs/");
			File.WriteAllText("./docs/CommandsList.md", helpstr.ToString());
			await ReplyConfirmLocalized("commandlist_regen").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		[OwnerOnly]
		public Task GenerateCommandsList() {
			var cmds = _cmds.Commands.Select(c => (model: CommandStrings.GetCommandStringModel(c.Name), cmd: c)).GroupBy(m => m.cmd.Module.IsSubmodule ? m.cmd.Module.Parent : m.cmd.Module).OrderBy(g => g.Key.Name).ToDictionary(k => k.Key.Name.ToLower(), k => k.Select(c => c.model).Distinct().OrderBy(c => c.Name).ToArray());

			Directory.CreateDirectory("./docs/");
			var serializer = new SerializerBuilder().DisableAliases().Build();
			File.WriteAllText("./docs/commandstrings.yaml", serializer.Serialize(cmds));
			return Task.CompletedTask;
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task Guide() {
			await ConfirmLocalized("guide", "https://github.com/Midnight-Myth/Mitternacht-NEW/blob/master/docs/CommandsList.md").ConfigureAwait(false);
		}

		[MitternachtCommand, Usage, Description, Aliases]
		public async Task Donate() {
			await ReplyConfirmLocalized("donate", PatreonUrl).ConfigureAwait(false);
		}
	}

	public class CommandTextEqualityComparer : IEqualityComparer<CommandInfo> {
		public bool Equals(CommandInfo x, CommandInfo y) => x.Aliases.First() == y.Aliases.First();

		public int GetHashCode(CommandInfo obj) => obj.Aliases.First().GetHashCode();
	}

	public class ModuleOrderComparer : IComparer<string> {
		private readonly StringComparer _sc = StringComparer.OrdinalIgnoreCase;

		public int Compare(string x, string y)
			=> x == "0" ? -1 : (y == "0" ? 1 : _sc.Compare(x, y));
	}
}
