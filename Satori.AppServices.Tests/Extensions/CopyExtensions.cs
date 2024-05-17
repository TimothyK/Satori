namespace Satori.AppServices.Tests.Extensions;

internal static class CopyExtensions
{
    public static T Copy<T>(this T source) where T:class
    {
        ArgumentNullException.ThrowIfNull(source);

        var type = source.GetType();
        var target = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Could not copy object");
        foreach (var property in type.GetProperties())
        {
            if (property.CanWrite)
            {
                property.SetValue(target, property.GetValue(source));
            }
        }

        return (T)target;
    }

    public static int GetNextId<T>(this IEnumerable<T> source, Func<T, int> idSelector)
    {
        return source.Select(idSelector).DefaultIfEmpty().Max() + 1;
    }
}