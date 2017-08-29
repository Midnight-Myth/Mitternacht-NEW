using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NCalc;

namespace NadekoBot.Modules.Utility
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
                    await Context.Channel.SendConfirmAsync("⚙ " + GetText("result"), expression.Trim() + "\n" + result);
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
                var functions = from m in typeof(CustomNCalcEvaluations).GetTypeInfo().GetMethods()
                    where m.IsStatic && m.IsPublic && m.ReturnType == typeof(void) && 
                          m.GetParameters().Length == 3 &&
                          m.GetParameters()[0].ParameterType == typeof(ICommandContext) &&
                          m.GetParameters()[1].ParameterType == typeof(DbService) &&
                          m.GetParameters()[2].ParameterType == typeof(FunctionArgs)
                    select m;
                var method = functions.FirstOrDefault(m => m.Name.ToLowerInvariant().Equals(name));
                method?.Invoke(null, new object[]{Context, _db, args});
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
                    });
                await Context.Channel.SendConfirmAsync(GetText("calcops", Prefix), string.Join(", ", selection));
            }
        }

        private class CustomNCalcEvaluations
        {
            public static void Level(ICommandContext context, DbService db, ref FunctionArgs args)
            {
                if (args.Parameters.Length > 1) return;
                var user = context.User as IGuildUser;
                if (args.Parameters.Length == 1)
                {
                    var parameter = args.Parameters[0];
                    if (parameter.ParsedExpression == null)
                        parameter.Evaluate();
                    var expr = parameter.ParsedExpression.ToString().Trim('[', ']');
                    context.Channel.SendMessageAsync($"expr: \"{expr}\"").GetAwaiter().GetResult();
                    if (ulong.TryParse(expr, out ulong id)) {
                        user = context.Guild.GetUserAsync(id).GetAwaiter().GetResult();
                    }
                    else {
                        context.Channel.SendMessageAsync($"usernames: {context.Guild.GetUsersAsync().GetAwaiter().GetResult().Aggregate("", (s,u) => $"{s}{u.Username}+{u.Mention.Substring(1)}+{u.Nickname} | ", s => s.Substring(0, s.Length - 3))}").GetAwaiter().GetResult();
                        user = context.Guild.GetUsersAsync().GetAwaiter().GetResult()
                            .FirstOrDefault(u => u.Username == expr || u.Mention == expr);
                    }
                }
                context.Channel.SendMessageAsync($"user: {(user == null ? "null" : "notnull")}, {user?.Username}")
                    .GetAwaiter().GetResult();
                if (user == null) return;

                using (var uow = db.UnitOfWork)
                {
                    args.Result = uow.LevelModel.GetLevel(user.Id);
                }
            }

            public static void Cash(ICommandContext context, DbService db, ref FunctionArgs args)
                => Money(context, db, ref args);
            
            public static void Money(ICommandContext context, DbService db, ref FunctionArgs args)
            {
                if (args.Parameters.Length > 1)
                    return;
                var user = context.User as IGuildUser;
                if (args.Parameters.Length == 1) {
                    var parameter = args.Parameters[0];
                    if (parameter.ParsedExpression == null)
                        parameter.Evaluate();
                    var expr = parameter.ParsedExpression.ToString();
                    if (ulong.TryParse(expr, out ulong id)) {
                        user = context.Guild.GetUserAsync(id).GetAwaiter().GetResult();
                    }
                    else {
                        user = context.Guild.GetUsersAsync().GetAwaiter().GetResult()
                            .FirstOrDefault(u => u.Username == expr || u.Mention == expr);
                    }
                }
                if (user == null)
                    return;

                using (var uow = db.UnitOfWork)
                {
                    args.Result = uow.Currency.GetUserCurrency(user.Id);
                }
            }

            public static void Xp(ICommandContext context, DbService db, ref FunctionArgs args)
            {
                if (args.Parameters.Length > 1)
                    return;
                var user = context.User as IGuildUser;
                if (args.Parameters.Length == 1) {
                    var parameter = args.Parameters[0];
                    if (parameter.ParsedExpression == null)
                        parameter.Evaluate();
                    var expr = parameter.ParsedExpression.ToString();
                    if (ulong.TryParse(expr, out ulong id)) {
                        user = context.Guild.GetUserAsync(id).GetAwaiter().GetResult();
                    }
                    else {
                        user = context.Guild.GetUsersAsync().GetAwaiter().GetResult()
                            .FirstOrDefault(u => u.Username == expr || u.Mention == expr);
                    }
                }
                if (user == null)
                    return;

                using (var uow = db.UnitOfWork) {
                    args.Result = uow.LevelModel.GetXp(user.Id);
                }
            }
        }

        private class MethodInfoEqualityComparer : IEqualityComparer<MethodInfo>
        {
            public bool Equals(MethodInfo x, MethodInfo y) => x.Name == y.Name;

            public int GetHashCode(MethodInfo obj) => obj.Name.GetHashCode();
        }
    }
}