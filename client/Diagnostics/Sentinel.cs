using System;
using CitizenFX.Core;
using JetBrains.Annotations;
using Moonlight.Shared.Internal.Extensions;
using Moonlight.Shared.Internal.Json;

namespace Moonlight.Client.Internal.Diagnostics
{
    [PublicAPI]
    public static class Sentinel
    {
        public static void Capture(Exception exception) => Capture(null, exception);

        public static void Capture(string message, Exception exception)
        {
            BaseScript.TriggerServerEvent("moonlight_sentinel_exception", exception.ToSnapshot().ToJson());
            Logger.Error(message, exception);
        }
    }
}