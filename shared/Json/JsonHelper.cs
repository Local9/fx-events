using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Moonlight.Shared.Internal.Json.Contracts;
using Moonlight.Shared.Internal.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Moonlight.Shared.Internal.Json
{
    [PublicAPI]
    public static class JsonHelper
    {
        public static readonly Dictionary<Type, Type> Substitutes = new Dictionary<Type, Type>();
        private static SnowflakeConverter _snowflakeConverter = new SnowflakeConverter();

        public static readonly List<JsonConverter> Converters = new List<JsonConverter>
        {
            _snowflakeConverter
        };

        public static readonly JsonSerializerSettings Empty = new JsonSerializerSettings
        {
            Converters = Converters,
            ContractResolver = new ContractResolver()
        };

        public static readonly JsonSerializerSettings LowerCaseSettings = new JsonSerializerSettings
        {
            Converters = Converters,
            ContractResolver = new ContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string ToJson(this object value, bool pretty = false, SnowflakeRepresentation representation = SnowflakeRepresentation.String,
            JsonSerializerSettings settings = null)
        {
            return (string) InvokeWithRepresentation(
                () => JsonConvert.SerializeObject(value, pretty ? Formatting.Indented : Formatting.None, settings ?? Empty), representation);
        }

        public static T FromJson<T>(this string serialized, SnowflakeRepresentation representation = SnowflakeRepresentation.String,
            JsonSerializerSettings settings = null) => (T) FromJsonInternal(serialized, typeof(T), out _, representation, settings);

        public static object FromJson(this string serialized, Type type, SnowflakeRepresentation representation = SnowflakeRepresentation.String,
            JsonSerializerSettings settings = null) => FromJsonInternal(serialized, type, out _, representation, settings);

        public static T FromJson<T>(this string serialized, out bool result, SnowflakeRepresentation representation = SnowflakeRepresentation.String,
            JsonSerializerSettings settings = null)
        {
            var value = FromJsonInternal(serialized, typeof(T), out var transient, representation, settings);

            result = transient;
            return (T) value;
        }

        private static object FromJsonInternal(string serialized, Type type, out bool result, SnowflakeRepresentation representation,
            JsonSerializerSettings settings)
        {
            try
            {
                var deserialized = InvokeWithRepresentation(() => JsonConvert.DeserializeObject(serialized, type, settings ?? Empty),
                    representation, false);

                result = true;

                return deserialized;
            }
            catch (Exception)
            {
                result = false;

                throw;
            }
        }

        private static object InvokeWithRepresentation(Func<object> func, SnowflakeRepresentation representation, bool suppressErrors = true)
        {
            var transient = _snowflakeConverter.Representation;

            _snowflakeConverter.Representation = representation;

            try
            {
                return func.Invoke();
            }
            catch (Exception)
            {
                if (!suppressErrors)
                    throw;
            }

            _snowflakeConverter.Representation = transient;

            return null;
        }
    }
}