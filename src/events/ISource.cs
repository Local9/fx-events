using JetBrains.Annotations;

namespace Lusive.Events
{
    [PublicAPI]
    public interface ISource
    {
        int Handle { get; }
    }
}