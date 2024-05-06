using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeMonkeyProjectiles.Linq
{
    /// <summary>
    /// None extension method.  Logical Not of Any
    /// </summary>
    public static class NoneExtensions
    {
        /// <summary>
        /// Returns true if the enumeration is empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool None<T>(this IEnumerable<T> source) =>
            !source.Any();

        /// <summary>
        /// Returns true if no elements in the enumeration satisfy the given predicate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate">Function to match elements of the enumeration against</param>
        /// <returns></returns>
        public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate) =>
            !source.Any(predicate);

    }
}