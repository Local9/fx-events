using JetBrains.Annotations;

namespace Moonlight.Events.Diagnostics
{
    [PublicAPI]
    public interface IEventLogger
    {
        void Debug(params object[] values);
        void Info(params object[] values);
    }
}