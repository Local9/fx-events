using JetBrains.Annotations;
using Moonlight.Snowflakes;

namespace Moonlight.Events
{
    [PublicAPI]
    public interface IMessage
    {
        Snowflake Id { get; set; }
        string Signature { get; set; }
    }
}