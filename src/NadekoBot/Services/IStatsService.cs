using System;
using System.Threading.Tasks;

namespace Mitternacht.Services
{
    public interface IStatsService : INService
    {
        (ulong userId, string backupName)[] AuthorIdBackupNames { get; }
        string Author { get; }
        long CommandsRan { get; }
        string Heap { get; }
        string Library { get; }
        long MessageCounter { get; }
        double MessagesPerSecond { get; }
        long TextChannels { get; }
        long VoiceChannels { get; }
        int GuildCount { get; }

		TimeSpan Uptime { get; }
		string GetUptimeString(string separator = ", ");
        void Initialize();
    }
}
