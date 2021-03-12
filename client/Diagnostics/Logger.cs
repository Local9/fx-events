using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Moonlight.Client.Internal.Pulse;
using Moonlight.Shared.Internal.Diagnostics;

namespace Moonlight.Client.Internal.Diagnostics
{
    [PublicAPI]
    public class Logger : BaseLogger
    {
        public static readonly List<string> Buffer = new List<string>();

        private PulseInstance _pulse = new PulseInstance();

        public Logger()
        {
        }

        public Logger(string source) : base(source)
        {
        }

        protected override void Write(Severity severity, string value)
        {
            CitizenFX.Core.Debug.WriteLine(value);
            
            Buffer.Add(value);
            _pulse.Mutate("panel.console.BUFFER", Buffer);
        }

        public new static void Info(params object[] values) => MoonClient.CurrentInstance.Logger.Info(values);
        public new static void Debug(params object[] values) => MoonClient.CurrentInstance.Logger.Debug(values);
        public new static void Error(string message, Exception exception = null) => MoonClient.CurrentInstance.Logger.Error(message, exception);
    }
}