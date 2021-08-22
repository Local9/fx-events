using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lusive.Events.Diagnostics;
using Lusive.Events.Exceptions;
using Lusive.Events.Message;
using Lusive.Events.Models;
using Lusive.Events.Serialization;

namespace Lusive.Events
{
    public delegate Task EventDelayMethod(int ms = 0);

    public delegate Task EventMessagePreparation(string pipeline, ISource source, IMessage message);

    public delegate void EventMessagePush(string pipeline, ISource source, byte[] buffer);

    [PublicAPI]
    public abstract class BaseGateway
    {
        protected abstract IEventLogger Logger { get; }
        protected abstract ISerialization Serialization { get; }

        private List<Tuple<EventMessage, EventHandler>> _processed = new();
        private List<EventObservable> _queue = new();
        private List<EventHandler> _handlers = new();

        public delegate void EventPipeline(PipelineEvent type, NetworkEvent value);

        public event EventPipeline Pipeline;

        public EventDelayMethod? DelayDelegate { get; set; }
        public EventMessagePreparation? PrepareDelegate { get; set; }
        public EventMessagePush? PushDelegate { get; set; }

        public async Task ProcessInvokeAsync(ISource source, byte[] serialized)
        {
            using var context =
                new SerializationContext(EventConstant.InvokePipeline, "(Process) In", Serialization, serialized);
            var message = context.Deserialize<EventMessage>();

            await ProcessInvokeAsync(message, source);
        }

        public async Task ProcessInvokeAsync(EventMessage message, ISource source)
        {
            var structure = new NetworkEvent(EventConstant.ServerSender, message);
            
            Pipeline.Invoke(PipelineEvent.Received, structure);
            
            if (message.FlowType == EventFlowType.Circular)
            {
                var stopwatch = StopwatchUtil.StartNew();
                var subscription = _handlers.SingleOrDefault(self => self.Endpoint == message.Endpoint) ??
                                   throw new Exception($"Could not find a handler for endpoint '{message.Endpoint}'");
                var result = InvokeDelegate(subscription);

                if (result.GetType().GetGenericTypeDefinition() == typeof(Task<>))
                {
                    using var token = new CancellationTokenSource();

                    var task = (Task) result;
                    var timeout = DelayDelegate!(10000);
                    var completed = await Task.WhenAny(task, timeout);

                    if (completed == task)
                    {
                        token.Cancel();

                        await task.ConfigureAwait(false);

                        result = (object) ((dynamic) task).Result;
                    }
                    else
                    {
                        throw new EventTimeoutException(
                            $"({message.Endpoint} - {subscription.Delegate.Method.DeclaringType?.Name ?? "null"}/{subscription.Delegate.Method.Name}) The operation was timed out");
                    }
                }

                var resultType = result?.GetType() ?? typeof(object);
                var response = new EventResponseMessage(message.Id, message.Endpoint, message.Signature, null);

                if (result != null)
                {
                    using var context = new SerializationContext(message.Endpoint, "(Process) Result", Serialization);

                    context.Serialize(resultType, result);
                    response.Data = context.GetData();
                }
                else
                {
                    response.Data = Array.Empty<byte>();
                }

                using (var context = new SerializationContext(message.Endpoint, "(Process) Response", Serialization))
                {
                    context.Serialize(response);

                    var data = context.GetData();

                    PushDelegate(EventConstant.ReplyPipeline, source, data);
                    Logger.Debug(
                        $"[{message.Endpoint}] Responded to {source} with {data.Length} byte(s) in {stopwatch.Elapsed.TotalMilliseconds}ms");
                    
                    structure.SetResponseData(result, GetCurrentTimestamp());
                    
                    Pipeline.Invoke(PipelineEvent.Response, structure);
                }
            }
            else
            {
                foreach (var handler in _handlers.Where(self => message.Endpoint == self.Endpoint))
                {
                    InvokeDelegate(handler);
                }
            }

            object InvokeDelegate(EventHandler subscription)
            {
                var parameters = new List<object>();
                var @delegate = subscription.Delegate;
                var method = @delegate.Method;
                var takesSource = method.GetParameters().Any(self => self.ParameterType == source.GetType());
                var startingIndex = takesSource ? 1 : 0;

                object CallInternalDelegate()
                {
                    return @delegate.DynamicInvoke(parameters.ToArray());
                }

                if (takesSource)
                {
                    parameters.Add(source);
                }

                if (message.Parameters == null) return CallInternalDelegate();

                var array = message.Parameters.ToArray();
                var holder = new List<object>();
                var parameterInfos = @delegate.Method.GetParameters();

                for (var idx = 0; idx < array.Length; idx++)
                {
                    var parameter = array[idx];
                    var type = parameterInfos[startingIndex + idx].ParameterType;

                    using var context = new SerializationContext(message.Endpoint, $"(Process) Parameter Index {idx}",
                        Serialization, parameter.Data);

                    holder.Add(context.Deserialize(type));
                }

                parameters.AddRange(holder.ToArray());

                return CallInternalDelegate();
            }
        }

        public void ProcessReply(byte[] serialized)
        {
            using var context =
                new SerializationContext(EventConstant.ReplyPipeline, "(Process) Out", Serialization, serialized);
            var response = context.Deserialize<EventResponseMessage>();

            ProcessReply(response);
        }

        public void ProcessReply(EventResponseMessage response)
        {
            var waiting = _queue.SingleOrDefault(self => self.Message.Id == response.Id) ??
                          throw new Exception($"No request matching {response.Id} was found.");

            _queue.Remove(waiting);
            waiting.Callback.Invoke(response.Data);
        }

        protected async Task<EventMessage> SendInternal(EventFlowType flow, ISource source, string endpoint,
            params object[] args)
        {
            var message = await CreateAndSendAsync(flow, source, endpoint, args);
            var structure = new NetworkEvent(EventConstant.LocalSender, message);

            Pipeline.Invoke(PipelineEvent.Sent, structure);

            return message;
        }

        protected async Task<T> GetInternal<T>(ISource source, string endpoint, params object[] args)
        {
            var stopwatch = StopwatchUtil.StartNew();
            var message = await CreateAndSendAsync(EventFlowType.Circular, source, endpoint, args);
            var structure = new NetworkEvent(EventConstant.LocalSender, message);
            var completion = new TaskCompletionSource<EventValueHolder<T>>();

            Pipeline.Invoke(PipelineEvent.Sent, structure);

            _queue.Add(new EventObservable(message, data =>
            {
                using var context = new SerializationContext(endpoint, "(Get) Response", Serialization, data);

                var holder = new EventValueHolder<T>
                {
                    Data = data,
                    Value = context.Deserialize<T>()
                };

                completion.TrySetResult(holder);
            }));

            await Task.WhenAny(completion.Task);

            var holder = await completion.Task;
            var elapsed = stopwatch.Elapsed.TotalMilliseconds;

            structure.SetResponseData(holder.Value, GetCurrentTimestamp());
            Pipeline.Invoke(PipelineEvent.Response, structure);

            Logger.Debug(
                $"[{message.Endpoint}] Received response from {source} of {holder.Data.Length} byte(s) in {elapsed}ms");

            return holder.Value;
        }

        public void Mount(string endpoint, Delegate @delegate)
        {
            Logger.Debug($"Mounted: {endpoint}");
            _handlers.Add(new EventHandler(endpoint, @delegate));
        }

        private async Task<EventMessage> CreateAndSendAsync(EventFlowType flow, ISource source, string endpoint,
            params object[] args)
        {
            var stopwatch = StopwatchUtil.StartNew();
            var parameters = new List<EventParameter>();

            for (var idx = 0; idx < args.Length; idx++)
            {
                var argument = args[idx];
                var type = argument.GetType();

                using var context = new SerializationContext(endpoint, $"(Send) Parameter Index '{idx}'",
                    Serialization);

                context.Serialize(type, argument);
                parameters.Add(new EventParameter(context.GetData()));
            }

            var message = new EventMessage(endpoint, flow, parameters);

            if (PrepareDelegate != null)
            {
                stopwatch.Stop();

                await PrepareDelegate(EventConstant.InvokePipeline, source, message);

                stopwatch.Start();
            }

            using (var context = new SerializationContext(endpoint, "(Send) Output", Serialization))
            {
                context.Serialize(message);

                var data = context.GetData();

                PushDelegate(EventConstant.InvokePipeline, source, data);
                Logger.Debug(
                    $"[{endpoint}] Sent {data.Length} byte(s) to {source} in {stopwatch.Elapsed.TotalMilliseconds}ms");

                return message;
            }
        }

        internal static long GetCurrentTimestamp() =>
            (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
    }
}