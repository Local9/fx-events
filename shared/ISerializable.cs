using JetBrains.Annotations;

namespace Moonlight.Shared.Internal.Events
{
    [PublicAPI]
    public interface ISerializable
    {
        string Signature { get; set; }
        string Serialize();
    }
}