using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;
using Mitternacht.Modules.Birthday.Models;

namespace Mitternacht.Common.TypeReaders
{
    public class BirthDateTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services) {
            input = input.Trim();
            var match = new Regex(@"(?:([0-9]+)\.([0-9]+)\.){1}([0-9]+)?").Match(input);
            try {
                if (match.Groups.Count >= 3) {
                    var day = int.Parse(match.Groups[1].Value);
                    var month = int.Parse(match.Groups[2].Value);
                    int? year = null;
                    if (match.Groups.Count >= 4 && !string.IsNullOrWhiteSpace(match.Groups[3].Value))
                        year = int.Parse(match.Groups[3].Value);
                    return Task.FromResult(TypeReaderResult.FromSuccess(new BirthDate(day, month, year)));
                }
            }
            catch (Exception e) {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.Exception, e.Message));
            }
            return Task.FromResult(TypeReaderResult.FromError(CommandError.Unsuccessful, "Regex did not match :/"));
        }
    }
}