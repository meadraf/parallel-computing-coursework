using System.Collections;

namespace InvertedIndexServer.InvertedIndex;

public class ConcurrentInvertedIndex<TKey, TValue>(int capacity = 30000) : IInvertedIndex<TKey, TValue>,
    IEnumerable<TKey>
    where TKey : notnull
{
    private readonly ConcurrentHashTable<TKey, ConcurrentHashSet<TValue>> _wordDictionary = new(capacity);

    public bool TryAdd(TKey key, TValue value)
    {
        if (_wordDictionary.TryGetValue(key, out var hashSet))
            return hashSet.Add(value);

        _wordDictionary.AddOrUpdate(key, [value]);

        return true;
    }

    public bool TryRemoveValue(TKey key, TValue value)
    {
        if (!_wordDictionary.TryGetValue(key, out var values)) return false;
        if (!values.Remove(value)) return false;
        if (values.Count == 0)
            _wordDictionary.Remove(key);

        return true;
    }

    public bool TryGetValue(TKey key, out ConcurrentHashSet<TValue>? values)
    {
        return _wordDictionary.TryGetValue(key, out values);
    }

    public bool ContainsKey(TKey key)
    {
        return _wordDictionary.Contains(key);
    }

    public IEnumerator<TKey> GetEnumerator()
    {
        var keysSnapshot = _wordDictionary.Select(t => t.Key).GetEnumerator();

        return keysSnapshot;
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}