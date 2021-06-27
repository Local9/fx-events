using JetBrains.Annotations;

namespace Moonlight.Events.Models
{
    [PublicAPI]
    public class EventValueHolder<T>
    {
        public byte[] Buffer { get; set; }
        public T Value { get; set; }
    }
}