using System;
using Discord.Commands;

namespace Mitternacht.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class NadekoModuleAttribute : GroupAttribute
    {
        public NadekoModuleAttribute(string moduleName) : base(moduleName)
        {
        }
    }
}

