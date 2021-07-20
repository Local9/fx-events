using JetBrains.Annotations;

namespace Lusive.Events.Message
{
    [PublicAPI]
    public enum EventFlowType
    {
        Straight,
        Circular
    }
}