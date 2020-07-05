using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;
using NCalc;

namespace Mitternacht.Modules.Utility {
	public partial class Utility
    {
        [Group]
        public class CalcCommands : MitternachtSubmodule
        {
            private readonly IUnitOfWork uow;

            private readonly Random _rnd;

            public CalcCommands(IUnitOfWork uow)
            {
                this.uow = uow;
                _rnd = new Random();
            }

            [MitternachtCommand, Usage, Description, Aliases]
            public async Task Calculate([Remainder] string expression)
            {
                var expr = new Expression(expression, EvaluateOptions.IgnoreCase);
                expr.EvaluateParameter += Expr_EvaluateParameter;
                expr.EvaluateFunction += Expr_EvaluateFunction;
                var result = expr.Evaluate();
                if (expr.Error == null)
                    await Context.Channel.SendConfirmAsync(expression.Replace("*", "\\*").Replace("_", "\\_").Trim() + "\n" + result, "⚙ " + GetText("result"));
                else
                    await Context.Channel.SendErrorAsync(expr.Error, "⚙ " + GetText("error"));
            }

            private static void Expr_EvaluateParameter(string name, ParameterArgs args)
            {
                switch (name.ToLowerInvariant())
                {
                    case "pi":
                        args.Result = Math.PI;
                        break;
                    case "e":
                        args.Result = Math.E;
                        break;
                }
            }

            private void Expr_EvaluateFunction(string name, FunctionArgs args)
            {
                name = name.ToLowerInvariant();
                // All methods have to be in CustomNCalcEvaluations and match the following template:
                // public static object calcfunctionname(ICommandContext context, IUnitOfWork uow, FunctionArgs args){ ... }
                var functions = from m in typeof(CustomNCalcEvaluations).GetTypeInfo().GetMethods()
                                where m.IsStatic && m.IsPublic && m.ReturnType == typeof(object) &&
                                      m.GetParameters().Length == 3 &&
                                      m.GetParameters()[0].ParameterType == typeof(ICommandContext) &&
                                      m.GetParameters()[1].ParameterType == typeof(IUnitOfWork) &&
                                      m.GetParameters()[2].ParameterType == typeof(FunctionArgs)
                                select m;
                var method = functions.FirstOrDefault(m => m.Name.ToLowerInvariant().Equals(name));
                var result = method?.Invoke(null, new object[] {Context, uow, args});
                if (result != null) args.Result = result;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            public async Task CalcOps()
            {
                var selection = typeof(Math).GetTypeInfo()
                    .GetMethods()
                    .Distinct(new MethodInfoEqualityComparer())
                    .Select(x => x.Name)
                    .Except(new[]
                    {
                        "ToString",
                        "Equals",
                        "GetHashCode",
                        "GetType"
                    }).ToArray();
                var functions = (from m in typeof(CustomNCalcEvaluations).GetTypeInfo().GetMethods()
                                 where m.IsStatic && m.IsPublic && m.ReturnType == typeof(object) &&
                                       m.GetParameters().Length == 3 &&
                                       m.GetParameters()[0].ParameterType == typeof(ICommandContext) &&
                                       m.GetParameters()[1].ParameterType == typeof(DbService) &&
                                       m.GetParameters()[2].ParameterType == typeof(FunctionArgs)
                                 select m.Name).ToArray();
                var eb = new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("calcops", Prefix));
                if (selection.Any()) eb.AddField("Math", string.Join(", ", selection));
                if (functions.Any()) eb.AddField("Custom", string.Join(", ", functions));
                await Context.Channel.EmbedAsync(eb);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            public async Task Rng(int upper = 1)
                => await Rng(0, upper).ConfigureAwait(false);

            [MitternachtCommand, Usage, Description, Aliases]
            public async Task Rng(int lower, int upper)
            {
                if(lower > upper)
                    (lower, upper) = (upper, lower);

                await ConfirmLocalized("rng", lower, upper, _rnd.Next(lower, upper+1)).ConfigureAwait(false);
            }
        }

        private class CustomNCalcEvaluations
        {
            //ulevel(user): level of a given user
            public static object ULevel(ICommandContext context, IUnitOfWork uow, FunctionArgs args)
            {
                //if (args.Parameters.Length > 0) context.Channel.SendMessageAsync($"args: {args.Parameters.Aggregate("", (s, p) => $"{s}{p.ParsedExpression.ToString()}, ", s => s.Substring(0, s.Length - 2))}")
                //    .GetAwaiter().GetResult();
                if (args.Parameters.Length > 1) return null;
                var user = context.User as IGuildUser;
                if (args.Parameters.Length == 1)
                {
                    var parameter = args.Parameters[0];
                    if (parameter.ParsedExpression == null) parameter.Evaluate();
                    var expr = parameter.ParsedExpression.ToString().Trim('[', ']', '\'', ' ');
                    //context.Channel.SendMessageAsync($"expr: {expr}").GetAwaiter().GetResult();
                    user = context.Guild.GetUserAsync(expr).GetAwaiter().GetResult();
                }
                //context.Channel.SendMessageAsync($"user: {(user == null ? "null" : "notnull")}, {user?.Username}").GetAwaiter().GetResult();
                if (user == null) return null;

				return uow.LevelModel.Get(context.Guild.Id, user.Id).Level;
			}

			//money(user): money of a given user
			public static object UMoney(ICommandContext context, IUnitOfWork uow, FunctionArgs args) {
				if(args.Parameters.Length > 1)
					return null;
				var user = context.User as IGuildUser;
				if(args.Parameters.Length == 1) {
					var parameter = args.Parameters[0];
					if(parameter.ParsedExpression == null)
						parameter.Evaluate();
					var expr = parameter.ParsedExpression.ToString().Trim('[', ']', '\'', ' ');
					user = context.Guild.GetUserAsync(expr).GetAwaiter().GetResult();
				}
				if(user == null)
					return null;

				return uow.Currency.GetUserCurrency(user.Id);
			}

			//xp(user): xp of a given user
			public static object UXp(ICommandContext context, IUnitOfWork uow, FunctionArgs args) {
				if(args.Parameters.Length > 1)
					return null;
				var user = context.User as IGuildUser;
				if(args.Parameters.Length == 1) {
					var parameter = args.Parameters[0];
					if(parameter.ParsedExpression == null)
						parameter.Evaluate();
					var expr = parameter.ParsedExpression.ToString().Trim('[', ']', '\'', ' ');
					user = context.Guild.GetUserAsync(expr).GetAwaiter().GetResult();
				}
				if(user == null)
					return null;

				return uow.LevelModel.Get(context.Guild.Id, user.Id).TotalXP;
			}

			//levelxp(lvl): xp needed to reach the given level beginning at level 0
			//levelxp(lvl1, lvl2): xp needed to get to lvl2 from lvl1
			public static object LevelXp(ICommandContext context, IUnitOfWork uow, FunctionArgs args) {
				if(args.Parameters.Length < 1 || args.Parameters.Length > 2)
					return null;
				var arg1 = args.Parameters[0];
				if(arg1.ParsedExpression == null)
					arg1.Evaluate();
				var expr = arg1.ParsedExpression.ToString();
				if(!int.TryParse(expr, out var lvl1))
					return null;
				if(args.Parameters.Length < 2)
					return LevelModel.GetXpForLevel(lvl1);

				var arg2 = args.Parameters[1];
				if(arg2.ParsedExpression == null)
					arg2.Evaluate();
				expr = arg2.ParsedExpression.ToString();
				return !int.TryParse(expr, out var lvl2) ? null : (object)(LevelModel.GetXpForLevel(lvl2) - LevelModel.GetXpForLevel(lvl1));
			}
		}

		private class MethodInfoEqualityComparer : IEqualityComparer<MethodInfo> {
			public bool Equals(MethodInfo x, MethodInfo y) => x.Name == y.Name;

			public int GetHashCode(MethodInfo obj) => obj.Name.GetHashCode();
		}
	}
}