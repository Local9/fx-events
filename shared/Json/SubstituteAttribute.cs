using System;
using JetBrains.Annotations;

namespace Moonlight.Shared.Internal.Json
{
    [PublicAPI]
    public class SubstituteAttribute : Attribute
    {
        public Type BackingType { get; set; }

        public SubstituteAttribute(Type backingType)
        {
            BackingType = backingType;
        }
    }
}