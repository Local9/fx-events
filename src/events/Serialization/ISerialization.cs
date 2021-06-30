using System;
using JetBrains.Annotations;

namespace Moonlight.Events.Serialization
{
    [PublicAPI]
    public interface ISerialization
    {
        void Serialize<T>(T value, SerializationContext context);
        object Deserialize(Type type, SerializationContext context);
        T Deserialize<T>(SerializationContext context);
    }
}