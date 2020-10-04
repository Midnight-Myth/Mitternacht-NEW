using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Database;
using Mitternacht.Database.Models;
using NCalc;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		[Group]
		public class CalcCommands : MitternachtSubmodule {
			private readonly IUnitOfWork uow;
			private readonly Random _rnd;

			public CalcCommands(IUnitOfWork uow) {
				this.uow = uow;
				_rnd = new Random();
			}

			[MitternachtCommand, Usage, Description, Aliases]
			public async Task Calculate([Remainder] string expression) {
				var expr = new Expression(expression, EvaluateOptions.IgnoreCase);
				expr.EvaluateParameter += Expr_EvaluateParameter;
				expr.EvaluateFunction += Expr_EvaluateFunction;
				var result = expr.Evaluate();

				if(expr.Error == null) {
					await Context.Channel.SendConfirmAsync($"{expression.Replace("*", "\\*").Replace("_", "\\_").Trim()}\n{result}", $"⚙ {GetText("result")}").ConfigureAwait(false);
				} else {
					await Context.Channel.SendErrorAsync(expr.Error, $"⚙ {GetText("error")}").ConfigureAwait(false);
				}
			}

			private static readonly MethodInfo[] CustomEvaluatorFunctions = (from m in typeof(CustomNCalcEvaluations).GetTypeInfo().GetMethods()
																			 where m.IsStatic && m.IsPublic && m.ReturnType == typeof(object)
																				&& m.GetParameters().Length == 3
																				&& m.GetParameters()[0].ParameterType == typeof(ICommandContext)
																				&& m.GetParameters()[1].ParameterType == typeof(IUnitOfWork)
																				&& m.GetParameters()[2].ParameterType == typeof(FunctionArgs)
																			 select m).ToArray();

			// All methods have to be in CustomNCalcEvaluations and match the following template:
			// public static object calcfunctionname(ICommandContext context, IUnitOfWork uow, FunctionArgs args){ ... }
			private static readonly string[] MathMethodsNames = typeof(Math).GetTypeInfo()
					.GetMethods()
					.Select(x => x.Name)
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.Except(new[] {
						"ToString",
						"Equals",
						"GetHashCode",
						"GetType"
					}).ToArray();

			private static void Expr_EvaluateParameter(string name, ParameterArgs args) {
				args.Result = name.Equals("pi", StringComparison.OrdinalIgnoreCase)
					? Math.PI
					: name.Equals("e", StringComparison.OrdinalIgnoreCase)
					? Math.E
					: args.Result;
			}

			private void Expr_EvaluateFunction(string name, FunctionArgs args) {
				var method = CustomEvaluatorFunctions.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
				var result = method?.Invoke(null, new object[] {Context, uow, args});

				if(result != null) {
					args.Result = result;
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			public async Task CalcOps() {
				var eb = new EmbedBuilder().WithOkColor()
					.WithTitle(GetText("calcops", Prefix))
					.AddField("Math", string.Join(", ", MathMethodsNames))
					.AddField("Custom", string.Join(", ", CustomEvaluatorFunctions.Select(m => m.Name).ToArray()));

				await Context.Channel.EmbedAsync(eb).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			public async Task Rng(int upper = 1)
				=> await Rng(0, upper).ConfigureAwait(false);

			[MitternachtCommand, Usage, Description, Aliases]
			public async Task Rng(int lower, int upper) {
				if(lower > upper)
					(lower, upper) = (upper, lower);

				await ConfirmLocalized("rng", lower, upper, _rnd.Next(lower, upper + 1)).ConfigureAwait(false);
			}
		}

		private class CustomNCalcEvaluations {
			//ulevel(user): level of a given user
			public static object ULevel(ICommandContext context, IUnitOfWork uow, FunctionArgs args) {
				if(args.Parameters.Length <= 1) {
					var user = context.User as IGuildUser;

					if(args.Parameters.Length == 1) {
						var parameter = args.Parameters[0];

						if(parameter.ParsedExpression == null)
							parameter.Evaluate();

						var expr = parameter.ParsedExpression.ToString().Trim('[', ']', '\'', ' ');
						user = context.Guild.GetUserAsync(expr).GetAwaiter().GetResult();
					}

					return user == null ? null : (object)uow.LevelModel.Get(context.Guild.Id, user.Id).Level;
				} else {
					return null;
				}
			}

			//money(user): money of a given user
			public static object UMoney(ICommandContext context, IUnitOfWork uow, FunctionArgs args) {
				if(args.Parameters.Length <= 1) {
					var user = context.User as IGuildUser;

					if(args.Parameters.Length == 1) {
						var parameter = args.Parameters[0];

						if(parameter.ParsedExpression == null) {
							parameter.Evaluate();
						}

						var expr = parameter.ParsedExpression.ToString().Trim('[', ']', '\'', ' ');
						user = context.Guild.GetUserAsync(expr).GetAwaiter().GetResult();
					}

					return user == null ? null : (object)uow.Currency.GetUserCurrencyValue(user.GuildId, user.Id);
				} else {
					return null;
				}
			}

			//xp(user): xp of a given user
			public static object UXp(ICommandContext context, IUnitOfWork uow, FunctionArgs args) {
				if(args.Parameters.Length <= 1) {
					var user = context.User as IGuildUser;

					if(args.Parameters.Length == 1) {
						var parameter = args.Parameters[0];

						if(parameter.ParsedExpression == null)
							parameter.Evaluate();

						var expr = parameter.ParsedExpression.ToString().Trim('[', ']', '\'', ' ');
						user = context.Guild.GetUserAsync(expr).GetAwaiter().GetResult();
					}

					return user == null ? null : (object)uow.LevelModel.Get(context.Guild.Id, user.Id).TotalXP;
				}

				return null;
			}

			//levelxp(lvl): xp needed to reach the given level beginning at level 0
			//levelxp(lvl1, lvl2): xp needed to get to lvl2 from lvl1
			public static object LevelXp(ICommandContext context, IUnitOfWork uow, FunctionArgs args) {
				if(args.Parameters.Length >= 1 && args.Parameters.Length <= 2) {
					var arg1 = args.Parameters[0];

					if(arg1.ParsedExpression == null)
						arg1.Evaluate();

					var expr = arg1.ParsedExpression.ToString();

					if(int.TryParse(expr, out var lvl1)) {
						if(args.Parameters.Length < 2) {
							return LevelModel.GetXpForLevel(lvl1);
						} else {
							var arg2 = args.Parameters[1];

							if(arg2.ParsedExpression == null)
								arg2.Evaluate();

							expr = arg2.ParsedExpression.ToString();

							return !int.TryParse(expr, out var lvl2) ? null : (object)(LevelModel.GetXpForLevel(lvl2) - LevelModel.GetXpForLevel(lvl1));
						}
					} else {
						return null;
					}
				} else {
					return null;
				}
			}
		}
	}
}