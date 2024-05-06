using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeMonkeyProjectiles.Linq
{
    /// <summary>
    /// IsIn and IsNotIn extension methods
    /// </summary>
    public static class IsInExtensions
    {
        #region IsIn

        /// <summary>
        /// Checks if a value exists within a collection by using the default equality comparer.  
        /// Functionally equivalent to Contains() but with the parameter order rearranged.
        /// </summary>
        /// <typeparam name="T">Type of values in the sequence</typeparam>
        /// <param name="target">Value to locate in the sequence</param>
        /// <param name="collection">The sequence to search in</param>
        /// <returns></returns>
        /// <remarks>
        /// This inverts the parameter order of Contains() to improve the readability.
        /// 
        /// Normally in an if statement the value under test is on the left 
        /// and the constant being compared against is on the right
        ///  <code>if (myValue == 1)</code>
        /// 
        /// If the values were inverted it would look weird.  This is a code smell known as "yoda conditions"
        ///  <code>if (1 == myValue)</code>
        /// 
        /// The Contains function suffers from the fact often when it is used it reads like a yoda condition
        ///  <code>if ((new [] {1, 2, 3, 5, 7}).Contains(possiblePrime)</code>
        /// 
        /// The IsIn function inverts this
        ///  <code>if (possiblePrime.IsIn(1, 2, 3, 5, 7))</code>
        /// 
        /// </remarks>
        public static bool IsIn<T>(this T target, IEnumerable<T> collection) =>
            collection.Contains(target);

        /// <summary>
        /// Checks if a value is equal to one of a set of acceptable values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsIn<T>(this T target, params T[] collection) =>
            collection.Contains(target);

        /// <summary>
        /// Checks if a value exists within a collection by using a given equality comparer.  
        /// </summary>
        /// <param name="target"></param>
        /// <param name="comparer"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsIn(this string target, StringComparer comparer, IEnumerable<string> collection) =>
            collection.Contains(target, comparer);

        /// <summary>
        /// Checks if a value exists within a collection by using a given equality comparer.  
        /// </summary>
        /// <param name="target"></param>
        /// <param name="comparer"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsIn(this string target, StringComparer comparer, params string[] collection) =>
            collection.Contains(target, comparer);

        /// <summary>
        /// Checks if a value exists within a collection by using a given equality comparer.  
        /// </summary>
        /// <param name="target"></param>
        /// <param name="comparer"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsIn<T>(this T target, IEqualityComparer<T> comparer, IEnumerable<T> collection) =>
            collection.Contains(target, comparer);

        /// <summary>
        /// Checks if a value exists within a collection by using a given equality comparer.  
        /// </summary>
        /// <param name="target"></param>
        /// <param name="comparer"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsIn<T>(this T target, IEqualityComparer<T> comparer, params T[] collection) =>
            collection.Contains(target, comparer);

        #endregion

        #region IsNotIn

        /// <summary>
        /// Checks if a value does not exist within a collection.  
        /// Functionally equivalent to !Contains() but with the parameter order rearranged.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsNotIn<T>(this T target, IEnumerable<T> collection) =>
            !collection.Contains(target);

        /// <summary>
        /// Checks if a value is not equal to one of a set of unacceptable values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsNotIn<T>(this T target, params T[] collection) =>
            !collection.Contains(target);

        /// <summary>
        /// Checks if a value does not exist within a collection by using a given equality comparer.  
        /// </summary>
        /// <param name="target"></param>
        /// <param name="comparer"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsNotIn(this string target, StringComparer comparer, IEnumerable<string> collection) =>
            !collection.Contains(target, comparer);

        /// <summary>
        /// Checks if a value does not exist within a collection by using a given equality comparer.  
        /// </summary>
        /// <param name="target"></param>
        /// <param name="comparer"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsNotIn(this string target, StringComparer comparer, params string[] collection) =>
            !collection.Contains(target, comparer);

        /// <summary>
        /// Checks if a value does not exist within a collection by using a given equality comparer.  
        /// </summary>
        /// <param name="target"></param>
        /// <param name="comparer"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsNotIn<T>(this T target, IEqualityComparer<T> comparer, IEnumerable<T> collection) =>
            !collection.Contains(target, comparer);

        /// <summary>
        /// Checks if a value does not exist within a collection by using a given equality comparer.  
        /// </summary>
        /// <param name="target"></param>
        /// <param name="comparer"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static bool IsNotIn<T>(this T target, IEqualityComparer<T> comparer, params T[] collection) =>
            !collection.Contains(target, comparer);

        #endregion

    }
}
