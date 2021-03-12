using System;
using JetBrains.Annotations;
using Moonlight.Shared.Internal.Diagnostics;

namespace Moonlight.Server.Internal.Diagnostics
{
    [PublicAPI]
    public class Logger : BaseLogger
    {
        public Logger()
        {
        }

        public Logger(string source) : base(source)
        {
        }

        protected override void Write(Severity severity, string value)
        {
            if (severity == Severity.Error)
                Console.ForegroundColor = ConsoleColor.Red;

            CitizenFX.Core.Debug.WriteLine(value);

            if (severity == Severity.Error)
                Console.ResetColor();
        }

        public new static void Info(params object[] values) => MoonServer.CurrentInstance.Logger.Info(values);
        public new static void Debug(params object[] values) => MoonServer.CurrentInstance.Logger.Debug(values);
        public new static void Error(string message, Exception exception = null) => MoonServer.CurrentInstance.Logger.Error(message, exception);
    }
}