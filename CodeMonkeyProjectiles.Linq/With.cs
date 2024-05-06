using System;

namespace CodeMonkeyProjectiles.Linq
{
    /// <summary>
    /// With extension method
    /// </summary>
    public static class WithExtensions
    {
        /// <summary>
        /// Performs an action on an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        /// <remarks>
        /// This is used to set a property of an object or some other action while still maintaining a fluent API syntax.
        /// 
        /// <example>
        /// <code>
        /// var person = _factory.CreatePerson()
        ///     .With(p => p.Name = "Jane Smith");
        /// </code>
        /// </example>
        /// </remarks>
        public static T With<T>(this T target, Action<T> action)
        {
            action(target);
            return target;
        }
    }
}