using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Moonlight.Shared.Internal;
using Moonlight.Shared.Internal.Diagnostics;
using Moonlight.Shared.Internal.Extensions;
using Moonlight.Shared.Internal.Json;
using Moonlight.Shared.Internal.Query;
using Newtonsoft.Json;

namespace Moonlight.Server.Internal.Diagnostics
{
    [PublicAPI]
    public static class Sentinel
    {
        private const string Webhook =
            "https://discord.com/api/webhooks/795237736205385738/n9Fog416jFdgFWBWNahZ9MI_jlyV5HJDzHf6dpzTzWGB5AD_1vq6ljubIEhvjf9wzbMp";

        public static void Capture(Exception exception) => Capture(null, exception);

        public static void Capture(string message, Exception exception)
        {
            var snapshot = exception.ToSnapshot();
            snapshot.TargetSite = exception.TargetSite;

            Capture(snapshot);
            Logger.Error(message, exception);
        }

        public static void Capture(ExceptionSnapshot exception, SentinelSource source = null)
        {
            var task = source != null ? Push(exception, source) : Push(exception);

            task.InvokeAndForget();
        }

        private static async Task Push(ExceptionSnapshot exception) => await Push(exception, new SentinelSource("Server", "Local"));

        private static async Task Push(ExceptionSnapshot exception, SentinelSource source)
        {
            var code = 0;
            var reason = string.Empty;

            try
            {
                var date = DateTime.Now;
                var full = exception.FlattenException();
                var query = full
                    .When(self => self.ContinuesWith(" in "))
                    .CollectWhile(self => !self.ContinuesWith(".cs:"))
                    .Collect(4)
                    .CollectWhile(self => char.IsNumber(self.Current))
                    .Result;

                var culprit = !string.IsNullOrEmpty(query) ? query.Substring(4) : null;
                var name = exception.Name;
                var method = exception.TargetSite as MethodInfo;
                var target = method != null
                    ? new[]
                    {
                        method.DeclaringType?.Name ?? "_",
                        method.DeclaringType?.Namespace ?? string.Empty,
                        FormatType(method.ReturnType),
                        method.Name,
                        Join(method.GetParameters().Select(self => self.ParameterType.Name + " " + self.Name))
                    }
                    : null;

                var embed = new
                {
                    Embeds = new[]
                    {
                        new
                        {
                            Author = new
                            {
                                Name = "Sentinel Cloud",
                                Icon_Url = "https://cdn.discordapp.com/avatars/794460040488026152/fc8dab337b6832fcd2c73beaa86b2fa9.png?size=256"
                            },
                            Title = $"@{exception.Source?.Remove(".net") ?? "*"}/{name}",
                            Timestamp = date,
                            Description =
                                $"*{exception.Message}*\n{(culprit != null ? $"<rider://{culprit.Replace(" ", "%20").Replace("\\", "/")}>" : "Could not find culprit")}\n\u200b",
                            Color = 0x33363c,
                            Footer = new
                            {
                                Text = "Moonlight"
                            },
                            Fields = new[]
                            {
                                new Field
                                {
                                    Name = "Affected Instances",
                                    Value = 1,
                                    Inline = true
                                },
                                new Field
                                {
                                    Name = "First Report",
                                    Value = "2021/01/11 22:22:34",
                                    Inline = true
                                },
                                new Field
                                {
                                    Name = "Source",
                                    Value = $"{source.Name} _{source.Identifier}_"
                                },
                                new Field
                                {
                                    Name = "\u200b",
                                    Value = "\u200b"
                                },
                                new Field
                                {
                                    Name = "Stack Trace",
                                    Value = "Currently Unavailable\n​"
                                },
                                new Field
                                {
                                    Name = "Target Site",
                                    Value = method != null
                                        ? $"```cs\n// {target[1]}\nclass {target[0]}\n{{\n\t{target[2]} {target[3]}({target[4]})\n}}```"
                                        : "N/A"
                                }
                            }
                        }
                    }
                };

                var content = new StringContent(embed.ToJson(false, SnowflakeRepresentation.UInt, JsonHelper.LowerCaseSettings), Encoding.UTF8,
                    "application/json")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                };

                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync(Webhook, content);

                    code = (int) response.StatusCode;
                    reason = response.ReasonPhrase;

                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception)
            {
                Logger.Error($"posting sentinel event failed: HTTPS {code} {reason}");
            }
        }

        private static string FormatType(Type type)
        {
            if (type.IsPrimitive || type.IsSpecialName || type.IsValueType || type == typeof(object))
            {
                return type.Name.ToLower();
            }

            if (type.IsGenericType)
            {
                var arguments = type.GetGenericArguments();

                return $"{type.Name.Replace($"`{arguments.Length}", string.Empty)}<{Join(arguments.Select(self => self.Name))}>";
            }

            return type.Name;
        }

        private static string Join(IEnumerable<object> enumerable)
        {
            return string.Join(", ", enumerable);
        }

        [PublicAPI]
        public class Field
        {
            public string Name { get; set; }

            [JsonIgnore] public object Value { get; set; }
            [JsonProperty("value")] public string SerializedValue => Value.ToString();

            public bool Inline { get; set; }
        }
    }
}