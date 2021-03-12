using JetBrains.Annotations;

namespace Moonlight.Server.Internal.Diagnostics
{
    [PublicAPI]
    public class SentinelSource
    {
        public string Name { get; set; }
        public string Identifier { get; set; }

        public SentinelSource(string name, string identifier)
        {
            Name = name;
            Identifier = identifier;
        }
    }
}