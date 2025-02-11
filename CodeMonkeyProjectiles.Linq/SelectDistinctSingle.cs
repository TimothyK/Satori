using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeMonkeyProjectiles.Linq
{
    public static class SelectDistinctSingleExtensions
    {
        public static TResult SelectDistinctSingle<TSource, TResult>(
            this IEnumerable<TSource> source
            , Func<TSource, TResult> selector
            , string objectName
        )
        {
            var values = source.Select(selector).Distinct().Take(2).ToArray();
            if (values.None())
            {
                throw new InvalidOperationException($"No {objectName} were found");
            }
            if (values.Length > 1)
            {
                throw new InvalidOperationException($"The {objectName} found were not unique");
            }

            return values.Single();
        }
    }
}
