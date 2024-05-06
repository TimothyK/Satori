using System.Collections.Generic;

namespace CodeMonkeyProjectiles.Linq
{
    /// <summary>
    /// Yield extension method
    /// </summary>
    public static class YieldExtensions
    {
        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt;
        /// consisting of a single item.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        /// <remarks>Taken from http://stackoverflow.com/questions/1577822/passing-a-single-item-as-ienumerablet
        /// </remarks>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }
    }
}