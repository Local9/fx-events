using JetBrains.Annotations;
using Moonlight.Shared.Internal.Events;

namespace Moonlight.Client.Internal.Events
{
    [PublicAPI]
    public class ServerSource : ISource
    {
        public int Handle => -1;
    }
}