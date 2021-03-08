using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Database;

namespace Mitternacht.Modules.Administration
{
	public partial class Administration
	{
		[Group]
		public class SqlCommands : MitternachtSubmodule
		{
			private readonly IUnitOfWork uow;

			public SqlCommands(IUnitOfWork uow)
			{
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[OwnerOnly]
			public async Task ExecSql([Remainder] string sql)
			{
				try
				{
					var msg = await Context.Channel.EmbedAsync(new EmbedBuilder()
							.WithOkColor()
							.WithTitle(GetText("sql_confirm"))
							.WithDescription(Format.Code(sql))
							.WithFooter("yes/no")).ConfigureAwait(false);

					var conf = await GetUserInputAsync(Context.User.Id, Context.Channel.Id) ?? "no";
					if (!conf.Equals("yes", StringComparison.OrdinalIgnoreCase) 
					    && !conf.Equals("y", StringComparison.OrdinalIgnoreCase))
					{
						return;
					}

					await msg.DeleteAsync().ConfigureAwait(false);

					var res = await uow.Context.Database.ExecuteSqlRawAsync(sql);
					await Context.Channel.SendConfirmAsync(res.ToString());
				}
				catch (Exception e)
				{
					await Context.Channel.SendErrorAsync(e.ToString());
				}
			}
		}
	}
}