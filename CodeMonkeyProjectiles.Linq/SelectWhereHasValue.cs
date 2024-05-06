using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeMonkeyProjectiles.Linq
{
    /// <summary>
    /// SelectWhereHasValue extension methods
    /// </summary>
    public static class SelectWhereHasValueExtensions
    {

        /// <summary>
        /// Selects the value of a collection of nullable values where the value is not null
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="values"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        /// <remarks>
        /// <example>Listing 1
        /// <code>
        /// //ProductID is a nullable int.  This gets the unique ProductID properties where the value isn't null
        /// int[] productIDs = orderLineItems.SelectWhereHasValue(oli => oli.ProductID).Distinct().ToArray();
        /// </code>
        /// </example>
        /// </remarks>
        public static IEnumerable<TResult> SelectWhereHasValue<TSource, TResult>(
            this IEnumerable<TSource> values,
            Func<TSource, TResult?> selector
        ) where TResult : struct =>
            values
                .Select(selector)
                .Where(x => x.HasValue)
                .Select(x => x.Value);


        /// <summary>
        /// Selects the value of a collection of string values where the value is not null or empty
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="values"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        /// <remarks>
        /// <example>Listing 1
        /// <code>
        /// //Key is a string.  This gets the unique Key properties where the value isn't null or empty string.
        /// string[] keys = orderLineItems.SelectWhereHasValue(oli => oli.Key).Distinct().ToArray();
        /// </code>
        /// </example>
        /// </remarks>
        public static IEnumerable<string> SelectWhereHasValue<TSource>(
            this IEnumerable<TSource> values,
            Func<TSource, string> selector
        ) =>
            values
                .Select(selector)
                .Where(x => !string.IsNullOrEmpty(x));

        /// <summary>
        /// Selects the value of a collection of objects where the value is not null
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="values"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        /// <remarks>
        /// <example>Listing 1
        /// <code>
        /// //Product is an object, which might be null.  This gets the unique Product properties where the value isn't null
        /// Product[] products = orderLineItems.SelectWhereHasValue(oli => oli.Product).Distinct().ToArray();
        /// </code>
        /// </example>
        /// </remarks>
        public static IEnumerable<TResult> SelectWhereHasValue<TSource, TResult>(
            this IEnumerable<TSource> values,
            Func<TSource, TResult> selector
        ) where TResult : class =>
            values
                .Select(selector)
                .Where(x => x != null);


    }
}
