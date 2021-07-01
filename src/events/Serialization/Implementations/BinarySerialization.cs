﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Moonlight.Events.Diagnostics;

namespace Moonlight.Events.Serialization.Implementations
{
    public delegate void SerializationObjectActivator(BinaryWriter writer);

    public delegate T DeserializationObjectActivator<out T>(BinaryReader reader);

    [PublicAPI]
    public class BinarySerialization : ISerialization
    {
        public const string PackMethod = "PackSerializedBytes";
        public IEventLogger Logger { get; set; }

        private delegate T ObjectActivator<out T>();

        private delegate void VoidMethod();

        public BinarySerialization(IEventLogger logger)
        {
            Logger = logger;
        }

        public void Serialize(Type type, object value, SerializationContext context)
        {
            var writer = context.Writer;

            try
            {
                if (type == typeof(object))
                {
                    throw new SerializationException("Cannot serialize values of 'System.Object' type");
                }

                var primitive = SerializePrimitive(type, value, context);

                if (primitive) return;

                if (value is IEnumerable enumerable)
                {
                    var generics = type.GetGenericArguments();

                    if (generics.Length == 0)
                    {
                        throw new SerializationException(
                            $"({type.FullName}) Cannot serialize non-generic IEnumerables.");
                    }

                    var generic = value is IDictionary
                        ? typeof(KeyValuePair<,>)
                            .MakeGenericType(generics[0], generics[1])
                        : generics[0];

                    var count = value switch
                    {
                        Array array => array.Length,
                        IDictionary dictionary => dictionary.Count,
                        IList list => list.Count,
                        ICollection collection => collection.Count,
                        _ => throw new SerializationException(
                            $"Enumerable type {type.FullName} is not supported. Try adding [Serialization] and the partial keyword, or manually implement packing/unpacking logic.")
                    };

                    writer.Write(count);

                    foreach (var entry in enumerable)
                    {
                        Serialize(generic, entry, context);
                    }
                }
                else if (GetTypeIdentifier(type) == "System.Collections.Generic.KeyValuePair`2")
                {
                    var generics = type.GetGenericArguments();
                    var method = GetType().GetMethod("Serialize",
                        new[] { typeof(Type), typeof(object), typeof(SerializationContext) });
                    var instanceParam = Expression.Parameter(typeof(BinarySerialization), "instance");
                    var typeParam = Expression.Parameter(typeof(Type), "type");
                    var contextParam = Expression.Parameter(typeof(SerializationContext), "context");
                    var pairParam = Expression.Parameter(type, "pair");
                    var valueParam = Expression.Parameter(typeof(object), "value");
                    var call = Expression.Call(instanceParam, method!, typeParam, valueParam, contextParam);

                    void CallSerialization(Type genericType, string property)
                    {
                        var action = (Action) Expression.Lambda(typeof(Action), Expression.Block(new[]
                            {
                                instanceParam,
                                typeParam,
                                contextParam,
                                pairParam,
                                valueParam
                            },
                            Expression.Assign(instanceParam, Expression.Constant(this, typeof(BinarySerialization))),
                            Expression.Assign(contextParam, Expression.Constant(context, typeof(SerializationContext))),
                            Expression.Assign(typeParam, Expression.Constant(genericType, typeof(Type))),
                            Expression.Assign(pairParam, Expression.Constant(value, type)),
                            Expression.Assign(valueParam,
                                Expression.Convert(Expression.Property(pairParam, property), typeof(object))),
                            call
                        )).Compile();

                        action.Invoke();
                    }

                    CallSerialization(generics[0], "Key");
                    CallSerialization(generics[1], "Value");
                }
                else
                    switch (value)
                    {
                        case DateTime date:
                            writer.Write(date.Ticks);

                            break;
                        case TimeSpan span:
                            writer.Write(span.Ticks);

                            break;
                        default:
                        {
                            var method = type.GetMethod(PackMethod, BindingFlags.Public | BindingFlags.Instance,
                                null,
                                CallingConventions.HasThis, new[] { typeof(BinaryWriter) }, null);

                            if (Equals(method, null))
                            {
                                throw new SerializationException(
                                    $"({type.FullName}) Failed to find \"{PackMethod}\" method; are you sure you have annotated the type with [Serialization] and the partial keyword?");
                            }

                            var parameter = Expression.Parameter(typeof(BinaryWriter), "writer");
                            var expression = Expression.Call(Expression.Constant(value, type), method, parameter);
                            var activator = (SerializationObjectActivator) Expression.Lambda(
                                typeof(SerializationObjectActivator), expression,
                                parameter).Compile();

                            activator.Invoke(context.Writer);

                            break;
                        }
                    }
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException($"Failed serialization of type '{type.FullName}'", ex);
            }
        }

        public object DeserializeAnonymously(Type type, SerializationContext context) =>
            Deserialize<object>(type, context);

        public T Deserialize<T>(Type type, SerializationContext context)
        {
            try
            {
                var primitive = DeserializePrimitive(type, context);

                if (primitive != null) return (T) primitive;

                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    var generics = type.GetGenericArguments();
                    var generic = typeof(IDictionary).IsAssignableFrom(type)
                        ? typeof(KeyValuePair<,>)
                            .MakeGenericType(generics[0], generics[1])
                        : generics[0];

                    var count = context.Reader.ReadInt32();
                    var countParam = Expression.Parameter(typeof(int), "count");
                    var lambda = (Func<object>) Expression.Lambda(
                        Expression.Block(new[] { countParam },
                            Expression.Assign(countParam, Expression.Constant(count, typeof(int))),
                            Expression.NewArrayBounds(generic, countParam)
                        )).Compile();
                    var arrayType = generic.MakeArrayType();
                    var array = lambda.Invoke();
                    var pointer = Expression.Constant(array, arrayType);
                    var method = GetType()
                        .GetMethod("DeserializeAnonymously", new[] { typeof(Type), typeof(SerializationContext) });
                    var instanceParam = Expression.Parameter(typeof(BinarySerialization), "instance");
                    var genericParam = Expression.Parameter(typeof(Type), "generic");
                    var contextParam = Expression.Parameter(typeof(SerializationContext), "context");

                    BlockExpression GetBlock(params Expression[] expressions)
                    {
                        return Expression.Block(new[]
                        {
                            instanceParam, genericParam, contextParam
                        }, new[]
                        {
                            Expression.Assign(instanceParam, Expression.Constant(this, typeof(BinarySerialization))),
                            Expression.Assign(genericParam, Expression.Constant(generic, typeof(Type))),
                            Expression.Assign(contextParam, Expression.Constant(context, typeof(SerializationContext)))
                        }.Concat(expressions));
                    }

                    for (var idx = 0; idx < count; idx++)
                    {
                        var call = Expression.Call(instanceParam, method!, genericParam, contextParam);
                        var idxAssign =
                            Expression.Assign(Expression.ArrayAccess(pointer, Expression.Constant(idx, typeof(int))),
                                Expression.Convert(call, generic));

                        ((VoidMethod) Expression.Lambda(typeof(VoidMethod), GetBlock(idxAssign)).Compile()).Invoke();
                    }

                    if (typeof(T).IsAssignableFrom(arrayType))
                    {
                        return (T) array;
                    }

                    var activator =
                        (ObjectActivator<T>) Expression.Lambda(typeof(ObjectActivator<T>), Expression.New(type))
                            .Compile();
                    var enumerable = activator.Invoke();

                    switch (enumerable)
                    {
                        case IDictionary _:
                        {
                            foreach (var pair in (Array) array)
                            {
                                var instance = Expression.Constant(enumerable, type);
                                var pairParam = Expression.Constant(pair, generic);
                                var keyParam = Expression.Parameter(generics[0], "key");
                                var valueParam = Expression.Parameter(generics[1], "value");
                                var call = Expression.Call(instance, type.GetMethod("Add",
                                        new[] { generics[0], generics[1] })!,
                                    keyParam,
                                    valueParam);
                                
                                var block = Expression.Block(new[]
                                    {
                                        keyParam,
                                        valueParam
                                    },
                                    Expression.Assign(keyParam, Expression.Property(pairParam, "Key")),
                                    Expression.Assign(valueParam, Expression.Property(pairParam, "Value")),
                                    call
                                );

                                var action = (Action) Expression.Lambda(typeof(Action), block).Compile();

                                action.Invoke();
                            }

                            break;
                        }
                        case IList list:
                        {
                            foreach (var entry in (Array) array)
                            {
                                list.Add(entry);
                            }

                            break;
                        }
                    }

                    return enumerable;
                }

                if (GetTypeIdentifier(type) == "System.Collections.Generic.KeyValuePair`2")
                {
                    var generics = type.GetGenericArguments();
                    var constructor = type.GetConstructor(generics) ??
                                      throw new SerializationException(
                                          $"Could not find suitable constructor for type: {type.FullName}");

                    var key = Deserialize<dynamic>(generics[0], context);
                    var value = Deserialize<dynamic>(generics[1], context);
                    var keyParam = Expression.Parameter(generics[0], "key");
                    var valueParam = Expression.Parameter(generics[1], "value");
                    var block = Expression.Block(
                        new[] { keyParam, valueParam },
                        Expression.Assign(keyParam, Expression.Constant(key, generics[0])),
                        Expression.Assign(valueParam, Expression.Constant(value, generics[1])),
                        Expression.New(constructor, keyParam, valueParam)
                    );

                    if (typeof(T) == typeof(object))
                    {
                        var generic = typeof(ObjectActivator<>).MakeGenericType(type);
                        var activator = Expression.Lambda(generic, block).Compile();

                        return activator.Invoke();
                    }
                    else
                    {
                        var activator =
                            (ObjectActivator<T>) Expression.Lambda(typeof(ObjectActivator<T>), block).Compile();

                        return activator.Invoke();
                    }
                }

                if (type == typeof(DateTime))
                {
                    return (T) (object) new DateTime(context.Reader.ReadInt64());
                }

                if (type == typeof(TimeSpan))
                {
                    return (T) (object) new TimeSpan(context.Reader.ReadInt64());
                }

                {
                    var constructor = type.GetConstructors().FirstOrDefault(self =>
                        self.GetParameters().FirstOrDefault()?.ParameterType == typeof(BinaryReader));

                    if (constructor == null)
                    {
                        throw new SerializationException(
                            $"Failed to find a suitable constructor with BinaryReader parameter in type: {type}");
                    }

                    var parameter = Expression.Parameter(typeof(BinaryReader), "reader");
                    var expression = Expression.New(constructor, parameter);
                    var activator = (DeserializationObjectActivator<T>) Expression
                        .Lambda(typeof(DeserializationObjectActivator<T>), expression, parameter).Compile();
                    var instance = activator.Invoke(context.Reader);

                    return instance;
                }
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException($"Failed deserialization of type '{type.FullName}'", ex);
            }
        }

        public bool SerializePrimitive(Type type, object value, SerializationContext context)
        {
            try
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        context.Writer.Write((bool) value);

                        return true;
                    case TypeCode.Byte:
                        context.Writer.Write((byte) value);

                        return true;
                    case TypeCode.Char:
                        context.Writer.Write((char) value);

                        return true;
                    case TypeCode.Double:
                        context.Writer.Write((double) value);

                        return true;
                    case TypeCode.Int16:
                        context.Writer.Write((short) value);

                        return true;
                    case TypeCode.Int32:
                        context.Writer.Write((int) value);

                        return true;
                    case TypeCode.Int64:
                        context.Writer.Write((long) value);

                        return true;
                    case TypeCode.Single:
                        context.Writer.Write((float) value);

                        return true;
                    case TypeCode.String:
                        context.Writer.Write((string) value);

                        return true;
                    case TypeCode.SByte:
                        context.Writer.Write((sbyte) value);

                        return true;
                    case TypeCode.UInt16:
                        context.Writer.Write((ushort) value);

                        return true;
                    case TypeCode.UInt32:
                        context.Writer.Write((uint) value);

                        return true;
                    case TypeCode.UInt64:
                        context.Writer.Write((ulong) value);

                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not serialize primitive: {type}", ex);
            }
        }

        public dynamic DeserializePrimitive(Type type, SerializationContext context)
        {
            try
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        return context.Reader.ReadBoolean();
                    case TypeCode.Byte:
                        return context.Reader.ReadByte();
                    case TypeCode.Char:
                        return context.Reader.ReadChar();
                    case TypeCode.Double:
                        return context.Reader.ReadDouble();
                    case TypeCode.Int16:
                        return context.Reader.ReadInt16();
                    case TypeCode.Int32:
                        return context.Reader.ReadInt32();
                    case TypeCode.Int64:
                        return context.Reader.ReadInt64();
                    case TypeCode.Single:
                        return context.Reader.ReadSingle();
                    case TypeCode.String:
                        return context.Reader.ReadString();
                    case TypeCode.SByte:
                        return context.Reader.ReadSByte();
                    case TypeCode.UInt16:
                        return context.Reader.ReadUInt16();
                    case TypeCode.UInt32:
                        return context.Reader.ReadUInt32();
                    case TypeCode.UInt64:
                        return context.Reader.ReadUInt64();
                }

                return default;
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not deserialize primitive: {type}", ex);
            }
        }

        public void Serialize<T>(T value, SerializationContext context) => Serialize(typeof(T), value, context);
        public object Deserialize(Type type, SerializationContext context) => Deserialize<object>(type, context);
        public T Deserialize<T>(SerializationContext context) => Deserialize<T>(typeof(T), context);

        private static string GetTypeIdentifier(Type type)
        {
            var builder = new StringBuilder();
            var declaring = type;

            builder.Append(type.Namespace);
            builder.Append(".");

            var idx = builder.Length;

            while ((declaring = declaring.DeclaringType) != null)
            {
                builder.Insert(idx, declaring.Name + ".");
            }

            builder.Append(type.Name);

            return builder.ToString();
        }
    }
}