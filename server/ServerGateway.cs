using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using JetBrains.Annotations;
using Moonlight.Server.Internal.Diagnostics;
using Moonlight.Shared;
using Moonlight.Shared.Internal.Diagnostics;
using Moonlight.Shared.Internal.Events;
using Moonlight.Shared.Internal.Events.Message;
using Moonlight.Shared.Internal.Extensions;
using Player = Moonlight.Server.Identity.Player;

namespace Moonlight.Server.Internal.Events
{
    [PublicAPI]
    public class ServerGateway : BaseGateway
    {
        protected override BaseLogger Logger { get; } = new Logger("Events");

        protected override Task GetDelayedTask(int milliseconds = 0)
        {
            return BaseScript.Delay(milliseconds);
        }

        protected override void TriggerImpl(string pipeline, int target, ISerializable payload)
        {
            if (target != ClientId.Global.Handle)
                BaseScript.TriggerClientEvent(MoonServer.GetPlayerFromHandle(target), pipeline, payload.Serialize());
            else
                BaseScript.TriggerClientEvent(pipeline, payload.Serialize());
        }

        private Dictionary<int, string> _signatures = new Dictionary<int, string>();

        public ServerGateway(IScriptBase script)
        {
            script.Hook(EventConstant.SignaturePipeline, new Action<string>(GetSignature));
            script.Hook(EventConstant.InboundPipeline, new Action<string, string>(Inbound));
            script.Hook(EventConstant.OutboundPipeline, new Action<string, string>(Outbound));
        }

        private void GetSignature([FromSource] string source)
        {
            try
            {
                var client = (ClientId)source;

                if (_signatures.ContainsKey(client.Handle))
                {
                    Logger.Info($"Client {client} tried obtaining event signature illegally.");

                    return;
                }

                var holder = new byte[64];

                using (var service = new RNGCryptoServiceProvider())
                {
                    service.GetBytes(holder);
                }

                var signature = BitConverter.ToString(holder).Replace("-", "").ToLower();

                _signatures.Add(client.Handle, signature);
                BaseScript.TriggerClientEvent(MoonServer.GetPlayerFromHandle(client.Handle), EventConstant.SignaturePipeline, signature);
            }
            catch (Exception ex)
            {
                Sentinel.Capture(ex);
            }
        }

        private async void Inbound([FromSource] string source, string serialized)
        {
            try
            {
                var client = (ClientId)source;

                if (!_signatures.TryGetValue(client.Handle, out var signature)) return;

                var message = EventMessage.Deserialize(serialized);

                if (message.Signature != signature)
                {
                    Logger.Info(
                        $"[{EventConstant.InboundPipeline}, {client.Handle}, {message.Signature}] Client {client} had invalid event signature, possible malicious intent?");

                    return;
                }

                try
                {
                    await ProcessInboundAsync(message, client);
                }
                catch (TimeoutException)
                {
                    API.DropPlayer(client.Handle.ToString(), $"Operation timed out: {message.Endpoint.ToBase64()}");
                }
            }
            catch (Exception ex)
            {
                Sentinel.Capture(ex);
            }
        }

        private void Outbound([FromSource] string source, string serialized)
        {
            try
            {
                var client = (ClientId)source;

                if (!_signatures.TryGetValue(client.Handle, out var signature)) return;

                var response = EventResponseMessage.Deserialize(serialized);

                if (response.Signature != signature)
                {
                    Logger.Info(
                        $"[{EventConstant.OutboundPipeline}, {client.Handle}, {response.Signature}] Client {client} had invalid event signature, possible malicious intent?");

                    return;
                }

                ProcessOutbound(response);
            }
            catch (Exception ex)
            {
                Sentinel.Capture(ex);
            }
        }

        public void Send(Player player, string endpoint, params object[] args) => Send(player.Handle, endpoint, args);
        public void Send(ClientId client, string endpoint, params object[] args) => Send(client.Handle, endpoint, args);

        public void Send(int target, string endpoint, params object[] args)
        {
            SendInternal(target, endpoint, args);
        }

        public Task<T> Get<T>(Player player, string endpoint, params object[] args) => Get<T>(player.Handle, endpoint, args);
        public Task<T> Get<T>(ClientId client, string endpoint, params object[] args) => Get<T>(client.Handle, endpoint, args);

        public async Task<T> Get<T>(int target, string endpoint, params object[] args)
        {
            return await GetInternal<T>(-1, endpoint, args);
        }
    }
}