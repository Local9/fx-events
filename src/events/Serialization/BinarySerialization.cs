using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Moonlight.Events.Diagnostics;

namespace Moonlight.Events.Serialization
{
    public delegate void SerializationObjectActivator(BinaryWriter writer);

    public delegate T DeserializationObjectActivator<out T>(BinaryReader reader);

    [PublicAPI]
    public class BinarySerialization : IEventSerialization
    {
        public const string PackMethod = "PackSerializedBytes";
        public IEventLogger Logger { get; set; }

        public BinarySerialization(IEventLogger logger)
        {
            Logger = logger;
        }

        public byte[] Serialize<T>(T value)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);

            var type = value?.GetType() ?? typeof(T);
            var method = type.GetMethod(PackMethod, BindingFlags.Public | BindingFlags.Instance, null,
                CallingConventions.HasThis, new[] { typeof(BinaryWriter) }, null);

            if (Equals(method, null))
            {
                throw new SerializationException(
                    $"({type.FullName}) Failed to find \"{PackMethod}\" method; are you sure you have annotated the type with [Serialization] and the partial keyword?");
            }

            try
            {
                var instance = Expression.Constant(value);
                var parameter = Expression.Parameter(typeof(BinaryWriter), "writer");
                var expression = Expression.Call(instance, method, parameter);
                var lambda = Expression.Lambda(typeof(SerializationObjectActivator), expression, parameter);
                var activator = (SerializationObjectActivator) lambda.Compile();

                activator(writer);
                
                return memory.ToArray();
            }
            catch (Exception ex)
            {
                throw new SerializationException($"Failed serialization of type '{type.FullName}'", ex);
            }
        }

        public T Deserialize<T>(Type type, byte[] buffer)
        {
            using var memory = new MemoryStream(buffer);
            using var reader = new BinaryReader(memory);

            if (type.IsPrimitive)
            {
                return (T) DeserializePrimitive(type, reader);
            }

            var constructor = type.GetConstructors().FirstOrDefault(self =>
                self.GetParameters().FirstOrDefault()?.ParameterType == typeof(BinaryReader));

            if (constructor == null)
            {
                throw new SerializationException(
                    $"Failed to find a suitable constructor with BinaryReader parameter in type: {type}");
            }

            try
            {
                var parameter = Expression.Parameter(typeof(BinaryReader), "reader");
                var expression = Expression.New(constructor, parameter);
                var lambda = Expression.Lambda(typeof(DeserializationObjectActivator<T>), expression, parameter);
                var activator = (DeserializationObjectActivator<T>) lambda.Compile();
                var instance = activator(reader);

                return instance;
            }
            catch (Exception ex)
            {
                throw new SerializationException($"Failed deserialization of type '{type.FullName}'", ex);
            }
        }

        public object Deserialize(Type type, byte[] buffer) => Deserialize<object>(type, buffer);
        public T Deserialize<T>(byte[] buffer) => Deserialize<T>(typeof(T), buffer);

        public dynamic DeserializePrimitive(Type type, BinaryReader reader)
        {
            try
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        return reader.ReadBoolean();
                    case TypeCode.Byte:
                        return reader.ReadByte();
                    case TypeCode.Char:
                        return reader.ReadChar();
                    case TypeCode.Decimal:
                        return reader.ReadDecimal();
                    case TypeCode.Double:
                        return reader.ReadDouble();
                    case TypeCode.Int16:
                        return reader.ReadInt16();
                    case TypeCode.Int32:
                        return reader.ReadInt32();
                    case TypeCode.Int64:
                        return reader.ReadInt64();
                    case TypeCode.Single:
                        return reader.ReadSingle();
                    case TypeCode.String:
                        return reader.ReadString();
                    case TypeCode.SByte:
                        return reader.ReadSByte();
                    case TypeCode.UInt16:
                        return reader.ReadUInt16();
                    case TypeCode.UInt32:
                        return reader.ReadUInt32();
                    case TypeCode.UInt64:
                        return reader.ReadUInt64();
                }

                return default;
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not deserialize primitive: {type}", ex);
            }
        }
    }
}