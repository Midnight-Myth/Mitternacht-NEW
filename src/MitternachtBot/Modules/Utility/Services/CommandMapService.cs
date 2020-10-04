using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Utility.Services {
	public class CommandMapService : IInputTransformer, IMService {
		private readonly Logger _log;
		private readonly DbService _db;

		public CommandMapService(DbService db) {
			_log = LogManager.GetCurrentClassLogger();
			_db = db;
		}

		public async Task<string> TransformInput(IGuild guild, IMessageChannel channel, IUser user, string input, bool realExecution = true) {
			await Task.Yield();

			if(guild == null || string.IsNullOrWhiteSpace(input))
				return input;

			input = input.ToLowerInvariant();

			using var uow = _db.UnitOfWork;
			var commandAliases = uow.GuildConfigs.For(guild.Id, set => set.Include(x => x.CommandAliases)).CommandAliases.Distinct(new CommandAliasEqualityComparer());

			if(!commandAliases.Any())
				return input;

			foreach(var ca in commandAliases.OrderByDescending(ca => ca.Trigger.Length)) {
				string newInput;
				if(input.StartsWith($"{ca.Trigger} ", StringComparison.OrdinalIgnoreCase))
					newInput = $"{ca.Mapping}{input[ca.Trigger.Length..]}";
				else if(input.Equals(ca.Trigger, StringComparison.OrdinalIgnoreCase))
					newInput = ca.Mapping;
				else
					continue;

				if(!realExecution)
					return newInput;

				_log.Info($"--Mapping Command--\nGuildId: {guild.Id}\nTrigger: {input}\nMapping: {newInput}");
				try { await channel.SendConfirmAsync($"{input} => {newInput}").ConfigureAwait(false); } catch { }
				return newInput;
			}

			return input;
		}
	}

	public class CommandAliasEqualityComparer : IEqualityComparer<CommandAlias> {
		public bool Equals(CommandAlias x, CommandAlias y)
			=> x.Trigger == y.Trigger;

		public int GetHashCode(CommandAlias obj) => obj.Trigger.GetHashCode();
	}
}
