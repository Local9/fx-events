using JetBrains.Annotations;

namespace Moonlight.Events
{
    [PublicAPI]
    public interface ISource
    {
        int Handle { get; }
    }
}