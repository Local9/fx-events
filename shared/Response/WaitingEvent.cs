using System;
using JetBrains.Annotations;
using Moonlight.Shared.Internal.Events.Message;

namespace Moonlight.Shared.Internal.Events.Response
{
    [PublicAPI]
    public class WaitingEvent
    {
        public EventMessage Message { get; set; }
        public Action<string> Callback { get; set; }

        public WaitingEvent(EventMessage message, Action<string> callback)
        {
            Message = message;
            Callback = callback;
        }
    }
}