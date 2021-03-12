using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using JetBrains.Annotations;
using Moonlight.Client.Internal.Diagnostics;
using Moonlight.Client.Internal.Pulse;
using Moonlight.Shared;
using Moonlight.Shared.Internal.Diagnostics;
using Moonlight.Shared.Internal.Events;
using Moonlight.Shared.Internal.Events.Message;
using Moonlight.Shared.Internal.Events.Response;
using Moonlight.Shared.Internal.Json;

namespace Moonlight.Client.Internal.Events
{
    [PublicAPI]
    public class ClientGateway : BaseGateway
    {
        public List<NetworkMessage> Buffer { get; } = new List<NetworkMessage>();

        protected override BaseLogger Logger { get; } = new Logger("Events");

        protected override Task GetDelayedTask(int milliseconds = 0)
        {
            return BaseScript.Delay(milliseconds);
        }

        protected override async void TriggerImpl(string pipeline, int target, ISerializable payload)
        {
            RequireServerTarget(target);

            // wait if the signature has not yet been delivered
            while (_signature == null)
            {
                await BaseScript.Delay(10);
            }

            // set the payload signature
            payload.Signature = _signature;
            BaseScript.TriggerServerEvent(pipeline, payload.Serialize());
        }

        private List<WaitingEvent> _queue = new List<WaitingEvent>();
        private List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private PulseInstance _pulse = new PulseInstance();
        private string _signature;

        public ClientGateway(IScriptBase script)
        {
            script.Hook(EventConstant.InboundPipeline, new Action<string>(Inbound));
            script.Hook(EventConstant.OutboundPipeline, new Action<string>(Outbound));
            script.Hook(EventConstant.SignaturePipeline, new Action<string>(TakeSignature));

            BaseScript.TriggerServerEvent(EventConstant.SignaturePipeline);
        }

        private void TakeSignature(string signature)
        {
            try
            {
                _signature = signature;
            }
            catch (Exception ex)
            {
                Sentinel.Capture(ex);
            }
        }

        private async void Inbound(string serialized)
        {
            try
            {
                var message = serialized.FromJson<EventMessage>();

                await ProcessInboundAsync(message, new ServerSource());
            }
            catch (Exception ex)
            {
                Sentinel.Capture(ex);
            }
        }

        private void Outbound(string serialized)
        {
            try
            {
                var response = EventResponseMessage.Deserialize(serialized);

                ProcessOutbound(response);
            }
            catch (Exception ex)
            {
                Sentinel.Capture(ex);
            }
        }

        public void Send(string endpoint, params object[] args)
        {
            SendInternal(-1, endpoint, args);
        }

        public async Task<T> Get<T>(string endpoint, params object[] args)
        {
            return await GetInternal<T>(-1, endpoint, args);
        }

        private void CommitBufferChanges()
        {
            _pulse.Mutate("panel.network.BUFFER", Buffer);
            _pulse.Mutate("panel.network.AVERAGE_LATENCY",
                (int)Math.Round(Buffer.Select(self => self.ResponseTime).Where(self => self != null).Average() ?? 0));
        }

        private void RequireServerTarget(int endpoint)
        {
            if (endpoint != -1) throw new Exception($"The client can only send to the server (arg {nameof(endpoint)} is not matching -1)");
        }
    }
}