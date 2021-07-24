using System;
using JetBrains.Annotations;

namespace Lusive.Events.Attributes
{
    /// <summary>
    /// Indicates that this property should be disregarded from serialization.
    /// </summary>
    [PublicAPI]
    public class IgnoreAttribute : Attribute
    {
    }
}