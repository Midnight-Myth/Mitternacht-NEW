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
using Mitternacht.Services.Database.Repositories.Impl;
using NCalc;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class CalcCommands : NadekoSubmodule
        {
            private readonly DbService _db;

            public CalcCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Calculate([Remainder] string expression)
            {
                var expr = new Expression(expression, EvaluateOptions.IgnoreCase);
                expr.EvaluateParameter += Expr_EvaluateParameter;
                expr.EvaluateFunction += Expr_EvaluateFunction;
                var result = expr.Evaluate();
                if (expr.Error == null)
                    await Context.Channel.SendConfirmAsync("⚙ " + GetText("result"), expression.Replace("*", "\\*").Replace("_", "\\_").Trim() + "\n" + result);
                else
                    await Context.Channel.SendErrorAsync("⚙ " + GetText("error"), expr.Error);
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
                // public static object calcfunctionname(ICommandContext context, DbService db, FunctionArgs args){ ... }
                var functions = from m in typeof(CustomNCalcEvaluations).GetTypeInfo().GetMethods()
                    where m.IsStatic && m.IsPublic && m.ReturnType == typeof(object) && 
                          m.GetParameters().Length == 3 &&
                          m.GetParameters()[0].ParameterType == typeof(ICommandContext) &&
                          m.GetParameters()[1].ParameterType == typeof(DbService) &&
                          m.GetParameters()[2].ParameterType == typeof(FunctionArgs)
                    select m;
                var method = functions.FirstOrDefault(m => m.Name.ToLowerInvariant().Equals(name));
                var result = method?.Invoke(null, new object[]{Context, _db, args});
                if (result != null) args.Result = result;
            }

            [NadekoCommand, Usage, Description, Aliases]
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
        }

        private class CustomNCalcEvaluations
        {
            private static object Level(ICommandContext context, DbService db, FunctionArgs args)
            {
                if (args.Parameters.Length > 0) context.Channel
                     .SendMessageAsync($"args: {args.Parameters.Aggregate("", (s, p) => $"{s}{p.ParsedExpression.ToString()}, ", s => s.Substring(0, s.Length - 2))}")
                     .GetAwaiter().GetResult();
                if (args.Parameters.Length > 1) return null;
                var user = context.User as IGuildUser;
                if (args.Parameters.Length == 1)
                {
                    var parameter = args.Parameters[0];
                    if (parameter.ParsedExpression == null)
                        parameter.Evaluate();
                    var expr = parameter.ParsedExpression.ToString().Trim('[', ']');
                    context.Channel.SendMessageAsync($"expr: \"{expr}\"").GetAwaiter().GetResult();
                    if (ulong.TryParse(expr, out var id)) {
                        user = context.Guild.GetUserAsync(id).GetAwaiter().GetResult();
                    }
                    else {
                        context.Channel.SendMessageAsync($"usernames: {context.Guild.GetUsersAsync().GetAwaiter().GetResult().Aggregate("", (s, u) => $"{s}{u.Username}+{u.Mention.Substring(1)}+{u.Nickname} | ", s => s.Substring(0, s.Length - 3))}").GetAwaiter().GetResult();
                        user = context.Guild.GetUsersAsync().GetAwaiter().GetResult()
                            .FirstOrDefault(u => u.Username == expr || u.Mention == expr);
                    }
                }
                context.Channel.SendMessageAsync($"user: {(user == null ? "null" : "notnull")}, {user?.Username}")
                    .GetAwaiter().GetResult();
                if (user == null) return null;

                using (var uow = db.UnitOfWork)
                {
                    return uow.LevelModel.GetLevel(user.Id);
                }
            }

            private static object Cash(ICommandContext context, DbService db, FunctionArgs args)
                => Money(context, db, args);

            private static object Money(ICommandContext context, DbService db, FunctionArgs args)
            {
                if (args.Parameters.Length > 1)
                    return null;
                var user = context.User as IGuildUser;
                if (args.Parameters.Length == 1) {
                    var parameter = args.Parameters[0];
                    if (parameter.ParsedExpression == null)
                        parameter.Evaluate();
                    var expr = parameter.ParsedExpression.ToString();
                    if (ulong.TryParse(expr, out var id)) {
                        user = context.Guild.GetUserAsync(id).GetAwaiter().GetResult();
                    }
                    else {
                        user = context.Guild.GetUsersAsync().GetAwaiter().GetResult()
                            .FirstOrDefault(u => u.Username == expr || u.Mention == expr);
                    }
                }
                if (user == null)
                    return null;

                using (var uow = db.UnitOfWork)
                {
                    return uow.Currency.GetUserCurrency(user.Id);
                }
            }

            private static object Xp(ICommandContext context, DbService db, FunctionArgs args)
            {
                if (args.Parameters.Length > 1)
                    return null;
                var user = context.User as IGuildUser;
                if (args.Parameters.Length == 1) {
                    var parameter = args.Parameters[0];
                    if (parameter.ParsedExpression == null)
                        parameter.Evaluate();
                    var expr = parameter.ParsedExpression.ToString();
                    if (ulong.TryParse(expr, out var id)) {
                        user = context.Guild.GetUserAsync(id).GetAwaiter().GetResult();
                    }
                    else {
                        user = context.Guild.GetUsersAsync().GetAwaiter().GetResult()
                            .FirstOrDefault(u => u.Username == expr || u.Mention == expr);
                    }
                }
                if (user == null)
                    return null;

                using (var uow = db.UnitOfWork) {
                    return uow.LevelModel.GetXp(user.Id);
                }
            }

            public static object XpForLvl(ICommandContext context, DbService db, FunctionArgs args) {
                if (args.Parameters.Length != 1) return null;
                var parameter = args.Parameters[0];
                if (parameter.ParsedExpression == null)
                    parameter.Evaluate();
                var expr = parameter.ParsedExpression.ToString();
                if (!int.TryParse(expr, out var lvl)) return null;
                return LevelModelRepository.GetXpForLevel(lvl);
            }
        }

        private class MethodInfoEqualityComparer : IEqualityComparer<MethodInfo>
        {
            public bool Equals(MethodInfo x, MethodInfo y) => x.Name == y.Name;

            public int GetHashCode(MethodInfo obj) => obj.Name.GetHashCode();
        }
    }
}