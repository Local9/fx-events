using System;
using JetBrains.Annotations;

namespace Moonlight.Events.Serialization
{
    [PublicAPI]
    public interface IEventSerialization
    {
        byte[] Serialize<T>(T value);
        object Deserialize(Type type, byte[] buffer);
        T Deserialize<T>(byte[] buffer);
    }
}