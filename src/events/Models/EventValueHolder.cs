﻿using JetBrains.Annotations;

namespace Lusive.Events.Models
{
    [PublicAPI]
    public class EventValueHolder<T>
    {
        public byte[] Data { get; set; }
        public T Value { get; set; }
    }
}