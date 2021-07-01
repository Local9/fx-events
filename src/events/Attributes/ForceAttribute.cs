using System;
using JetBrains.Annotations;

namespace Moonlight.Events.Attributes
{
    /// <summary>
    /// Indicates that this property should be forcefully added to serialization.
    /// </summary>
    [PublicAPI]
    public class ForceAttribute : Attribute
    {
    }
}