using System.Reflection;
using JetBrains.Annotations;

namespace Moonlight.Shared.Internal.Diagnostics
{
    [PublicAPI]
    public class ExceptionSnapshot
    {
        public string Name { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public MethodBase TargetSite { get; set; }
        public ExceptionSnapshot InnerException { get; set; }
    }
}