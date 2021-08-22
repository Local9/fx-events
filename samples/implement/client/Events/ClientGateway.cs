﻿using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using FxEvents.Samples.Implementation.Diagnostics;
using Lusive.Events;
using Lusive.Events.Diagnostics;
using Lusive.Events.Message;
using Lusive.Events.Serialization;
using Lusive.Events.Serialization.Implementations;

namespace FxEvents.Samples.Implementation.Events
{
    public class ClientGateway : BaseGateway
    {
        public const string SignaturePipeline = "moonlight_event_sig";

        protected sealed override IEventLogger Logger { get; }
        protected override ISerialization Serialization { get; }

        private string? _signature;

        public ClientGateway(Client client)
        {
            Logger = new Logger();
            Serialization = new BinarySerialization(Logger);
            DelayDelegate = async delay => await BaseScript.Delay(delay);
            PrepareDelegate = PrepareAsync;
            PushDelegate = Push;

            client.Hook(EventConstant.InvokePipeline,
                new Action<byte[]>(async serialized => { await ProcessInvokeAsync(new ServerId(), serialized); }));
            client.Hook(EventConstant.ReplyPipeline, new Action<byte[]>(ProcessReply));
            client.Hook(SignaturePipeline, new Action<string>(signature => _signature = signature));
            
            BaseScript.TriggerServerEvent(SignaturePipeline);
        }

        public async Task PrepareAsync(string pipeline, ISource source, IMessage message)
        {
            if (_signature == null)
            {
                var stopwatch = StopwatchUtil.StartNew();

                while (_signature == null)
                    await BaseScript.Delay(0);

                Logger.Debug($"[{message}] Signature fetch took {stopwatch.Elapsed.TotalMilliseconds}ms.");
            }

            message.Signature = _signature;
        }

        public void Push(string pipeline, ISource source, byte[] buffer)
        {
            if (source.Handle != -1)
                throw new Exception(
                    $"The client can only target the server. (arg {nameof(source)} is not matching -1)");

            BaseScript.TriggerServerEvent(pipeline, buffer);
        }

        public async void Send(string endpoint, params object[] args)
        {
            await SendInternal(EventFlowType.Straight, ServerId.Instance, endpoint, args);
        }

        public async Task<T> Get<T>(string endpoint, params object[] args)
        {
            return await GetInternal<T>(ServerId.Instance, endpoint, args);
        }
    }
}