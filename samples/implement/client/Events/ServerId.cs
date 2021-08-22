using Lusive.Events;

namespace FxEvents.Samples.Implementation.Events
{
    public class ServerId : ISource
    {
        public static readonly ServerId Instance = new();
        public int Handle => -1;

        public override string ToString()
        {
            return "server";
        }
    }
}