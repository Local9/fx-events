using JetBrains.Annotations;

namespace Moonlight.Shared.Internal.Events.Response
{
    [PublicAPI]
    public class EventValueHolder<T>
    {
        public T Value { get; set; }
    }
}