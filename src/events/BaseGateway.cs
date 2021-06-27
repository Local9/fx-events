using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Moonlight.Events.Diagnostics;
using Moonlight.Events.Message;
using Moonlight.Events.Models;
using Moonlight.Events.Serialization;

namespace Moonlight.Events
{
    public delegate Task EventDelayMethod(int ms = 0);

    public delegate Task EventMessagePreparation(string pipeline, ISource source, IMessage message);

    public delegate void EventMessagePush(string pipeline, ISource source, byte[] buffer);

    [PublicAPI]
    public abstract class BaseGateway
    {
        protected abstract IEventLogger Logger { get; }
        protected abstract IEventSerialization Serialization { get; }

        private List<Tuple<EventMessage, EventHandler>> _processed =
            new List<Tuple<EventMessage, EventHandler>>();

        private List<EventObservable> _queue = new List<EventObservable>();
        private List<EventHandler> _handlers = new List<EventHandler>();

        public EventDelayMethod DelayDelegate { get; set; }
        public EventMessagePreparation PrepareDelegate { get; set; }
        public EventMessagePush PushDelegate { get; set; }

        public async Task ProcessInboundAsync(ISource source, byte[] serialized)
        {
            var message = Serialization.Deserialize<EventMessage>(serialized);

            await ProcessInboundAsync(message, source);
        }

        public async Task ProcessInboundAsync(EventMessage message, ISource source)
        {
            object InvokeDelegate(EventHandler subscription)
            {
                var parameters = new List<object>();
                var @delegate = subscription.Delegate;
                var method = @delegate.Method;
                var takesSource = method.GetParameters().Any(self => self.ParameterType == source.GetType());
                var startingIndex = takesSource ? 1 : 0;

                if (takesSource)
                {
                    parameters.Add(source);
                }

                if (message.Parameters == null) return @delegate.DynamicInvoke(parameters.ToArray());

                var array = message.Parameters.ToArray();
                var holder = new List<object>();
                var parameterInfos = @delegate.Method.GetParameters();

                for (var index = 0; index < array.Length; index++)
                {
                    var parameter = array[index];
                    var type = parameterInfos[startingIndex + index].ParameterType;

                    holder.Add(Serialization.Deserialize(type, parameter.Data));
                }

                parameters.AddRange(holder.ToArray());

                return @delegate.DynamicInvoke(parameters.ToArray());
            }

            if (message.Flow == EventFlowType.Circle)
            {
                var stopwatch = StopwatchUtil.StartNew();
                var subscription = _handlers.SingleOrDefault(self => self.Endpoint == message.Endpoint) ??
                                   throw new Exception($"No handler for endpoint \"{message.Endpoint}\" was found.");
                var result = InvokeDelegate(subscription);

                if (result?.GetType().GetGenericTypeDefinition() == typeof(Task<>))
                {
                    using var token = new CancellationTokenSource();

                    var task = (Task) result;
                    var delay = Task.Run(async () => await DelayDelegate(30000), token.Token);
                    var completed = await Task.WhenAny(task, delay);

                    if (completed == task)
                    {
                        token.Cancel();

                        await task.ConfigureAwait(false);

                        result = (object) ((dynamic) task).Result;
                    }
                    else
                    {
                        throw new TimeoutException(
                            $"({message.Endpoint} - {subscription.Delegate.Method.DeclaringType?.Name ?? "null"}/{subscription.Delegate.Method.Name}) The operation was timed out.");
                    }
                }
                
                var response = new EventResponseMessage(message.Id, message.Signature, Serialization.Serialize(result));
                var buffer = Serialization.Serialize(response);

                PushDelegate(EventConstant.OutboundPipeline, source, buffer);
                Logger.Debug(
                    $"[{message.Endpoint}] Responded to \"{message.Id}\" with {buffer.Length} byte(s) in {stopwatch.Elapsed.TotalMilliseconds}ms");
            }
            else
            {
                foreach (var handler in _handlers.Where(self => message.Endpoint == self.Endpoint))
                {
                    InvokeDelegate(handler);
                }
            }
        }

        public void ProcessOutbound(byte[] serialized)
        {
            var response = Serialization.Deserialize<EventResponseMessage>(serialized);

            ProcessOutbound(response);
        }

        public void ProcessOutbound(EventResponseMessage response)
        {
            var waiting = _queue.SingleOrDefault(self => self.Message.Id == response.Id) ??
                          throw new Exception($"No request matching \"{response.Id}\" was found.");

            _queue.Remove(waiting);
            waiting.Callback.Invoke(response.Data);
        }

        protected async Task<EventMessage> SendInternal(EventFlowType flow, ISource source, string endpoint,
            params object[] args)
        {
            var stopwatch = StopwatchUtil.StartNew();
            var message = new EventMessage(endpoint, flow,
                args.Select(self => new EventParameter(Serialization.Serialize(self))));

            if (PrepareDelegate != null)
            {
                stopwatch.Stop();

                await PrepareDelegate(EventConstant.InboundPipeline, source, message);

                stopwatch.Start();
            }

            var buffer = Serialization.Serialize(message);

            PushDelegate(EventConstant.InboundPipeline, source, buffer);
            Logger.Debug(
                $"[{endpoint}] [{message.Id}] Sent {buffer.Length} byte(s) in {stopwatch.Elapsed.TotalMilliseconds}ms");

            return message;
        }

        protected async Task<T> GetInternal<T>(ISource source, string endpoint, params object[] args)
        {
            var stopwatch = StopwatchUtil.StartNew();
            var message = await SendInternal(EventFlowType.Circle, source, endpoint, args);
            var token = new CancellationTokenSource();
            var holder = new EventValueHolder<T>();

            _queue.Add(new EventObservable(message, response =>
            {
                holder.Buffer = response;
                holder.Value = Serialization.Deserialize<T>(response);

                token.Cancel();
            }));

            while (!token.IsCancellationRequested)
            {
                await DelayDelegate();
            }

            var elapsed = stopwatch.Elapsed.TotalMilliseconds;

            Logger.Debug(
                $"[{message.Endpoint}] [{message.Id}] [Task<{typeof(T).Name}>] Received response of {holder.Buffer.Length} byte(s) in {elapsed}ms");

            return holder.Value;
        }

        public void Mount(string endpoint, Delegate @delegate)
        {
            Logger.Debug($"Mounted: {endpoint}");
            _handlers.Add(new EventHandler(endpoint, @delegate));
        }
    }
}