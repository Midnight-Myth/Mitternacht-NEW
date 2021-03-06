﻿using System.Threading.Tasks;
using Discord;

namespace Mitternacht.Common.ModuleBehaviors
{
    /// <summary>
    /// Implemented by modules which block execution before anything is executed
    /// </summary>
    public interface IEarlyBlocker
    {
        Task<bool> TryBlockEarly(IGuild guild, IUserMessage msg, bool realExecution = true);
    }
}
