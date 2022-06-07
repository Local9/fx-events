using Lusive.Snowflake;

namespace Lusive.Events
{

    public interface IMessage
    {
        SnowflakeId Id { get; set; }
        string Endpoint { get; set; }
        string? Signature { get; set; }
    }
}