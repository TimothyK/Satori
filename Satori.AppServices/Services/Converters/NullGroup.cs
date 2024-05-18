using System.Collections;

namespace Satori.AppServices.Services.Converters;

internal class NullGroup<TKey, TValue>(TKey key) : IGrouping<TKey, TValue>
{
    public IEnumerator<TValue> GetEnumerator()
    {
        return new List<TValue>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public TKey Key { get; } = key;
}