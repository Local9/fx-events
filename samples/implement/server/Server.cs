using System;
using CitizenFX.Core;
using FxEvents.Samples.Implementation.Events;

namespace FxEvents.Samples.Implementation
{
    public class Server : BaseScript
    {
        private ServerGateway _events;
        
        public Server()
        {
            Instance = this;
            _events = new ServerGateway(this);
        }

        public void Hook(string eventName, Delegate @delegate)
        {
            EventHandlers[eventName] += @delegate;
        }

        public static Player ToPlayer(int handle)
        {
            return Server.Instance.Players[handle];
        }
        
        public static Server Instance { get; private set; }
    }
}