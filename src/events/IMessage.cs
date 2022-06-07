using JetBrains.Annotations;
using Lusive.Snowflake;

namespace Lusive.Events
{
    [PublicAPI]
    public interface IMessage
    {
        SnowflakeId Id { get; set; }
        string Endpoint { get; set; }
        string? Signature { get; set; }
    }
}