using System;
using JetBrains.Annotations;

namespace Moonlight.Events.Serialization
{
    [PublicAPI]
    public class SerializationException : Exception
    {
        public SerializationContext Context { get; set; }
        public Type InvolvedType { get; set; }

        public SerializationException(SerializationContext context, Type type, string message) : base(Format(context,
            type, message))
        {
            Context = context;
            InvolvedType = type;
        }

        public SerializationException(SerializationContext context, Type type, string message, Exception innerException)
            : base(Format(context, type, message), innerException)
        {
            Context = context;
            InvolvedType = type;
        }

        public static string Format(SerializationContext context, Type type, string message)
        {
            return $"{context.Source}: {(context.Details != null ? $"({context.Details}) " : string.Empty)}({type.FullName}) {message}";
        }
    }
}