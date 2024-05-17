using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeMonkeyProjectiles.Linq
{
    public static class SumExtensions
    {
        public static TimeSpan Sum(this IEnumerable<TimeSpan> values) => 
            values.Aggregate(TimeSpan.Zero, (total, value) => total + value);
    }
}
