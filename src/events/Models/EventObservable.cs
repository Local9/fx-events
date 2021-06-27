using System;
using JetBrains.Annotations;

namespace Moonlight.Events.Models
{
    [PublicAPI]
    public class EventObservable
    {
        public IMessage Message { get; set; }
        public Action<byte[]> Callback { get; set; }

        public EventObservable(IMessage message, Action<byte[]> callback)
        {
            Message = message;
            Callback = callback;
        }
    }
}