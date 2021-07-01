using System;
using JetBrains.Annotations;

namespace Moonlight.Events.Attributes
{
    /// <summary>
    /// Indicates that this property should be disregarded from serialization.
    /// </summary>
    [PublicAPI]
    public class IgnoreAttribute : Attribute
    {
    }
}