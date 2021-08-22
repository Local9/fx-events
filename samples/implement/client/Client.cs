using System;
using CitizenFX.Core;
using FxEvents.Samples.Implementation.Events;

namespace FxEvents.Samples.Implementation
{
    public class Client : BaseScript
    {
        private ClientGateway _events;
        
        public Client()
        {
            _events = new ClientGateway(this);
        }

        public void Hook(string eventName, Delegate @delegate)
        {
            EventHandlers[eventName] += @delegate;
        }
    }
}