using System;

namespace Mitternacht.Modules.Utility.Common.Exceptions
{
    public class StreamRoleNotFoundException : Exception
    {
        public StreamRoleNotFoundException() : base("Stream role wasn't found.")
        {
        }
    }
}
