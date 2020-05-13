using System.Threading.Tasks;

namespace Mitternacht.Modules.Games.Common.ChatterBot
{
    public interface IChatterBotSession
    {
        Task<string> Think(string input);
    }
}
