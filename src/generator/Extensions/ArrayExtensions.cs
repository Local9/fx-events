using System;
using System.Collections.Generic;
using System.Linq;

namespace Lusive.Events.Generator.Extensions
{
    public static class ArrayExtensions
    {
        public static T[] ConcatIf<T>(this IEnumerable<T> first, bool condition, params T[] value)
        {
            return condition ? first.Concat(value).ToArray() : first.ToArray();
        }
        
        public static T[] ConcatIf<T>(this IEnumerable<T> first, bool condition, Func<T[]> value)
        {
            return condition ? first.ConcatIf(true, value.Invoke()) : first.ToArray();
        }
    }
}