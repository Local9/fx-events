using JetBrains.Annotations;

namespace Moonlight.Shared.Internal.Events
{
    [PublicAPI]
    public interface ISource
    {
        int Handle { get; }
    }
}