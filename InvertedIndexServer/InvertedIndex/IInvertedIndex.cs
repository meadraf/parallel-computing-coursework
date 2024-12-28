using System.Globalization;

namespace InvertedIndexServer.InvertedIndex;

public interface IInvertedIndex<in TKey, TValue>
{
    public bool TryAdd(TKey key, TValue value);
    public bool TryGetValue(TKey key, out HashSet<TValue>? values);
    public bool ContainsKey(TKey key);
    public bool TryRemoveValue(TKey key, TValue value);
}