using System;
using JetBrains.Annotations;

namespace Moonlight.Events.Serialization
{
    [PublicAPI]
    public class SerializationException : Exception
    {
        public SerializationException(string message) : base(message)
        {
        }

        public SerializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}