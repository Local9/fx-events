using Lusive.Snowflake;
using System;

namespace Lusive.Events
{
    public class EventHandler
    {
        public SnowflakeId Id { get; set; }
        public string Endpoint { get; set; }
        public Delegate Delegate { get; set; }

        public EventHandler(string endpoint, Delegate @delegate)
        {
            Id = SnowflakeId.Next();
            Endpoint = endpoint;
            Delegate = @delegate;
        }
    }
}