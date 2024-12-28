namespace InvertedIndexServer.InvertedIndex;

public class ConcurrentInvertedIndex<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, HashSet<TValue>> _wordDictionary = new();
    private readonly object _indexLock = new();

    public bool TryAdd(TKey key, TValue value)
    {
        lock (_indexLock)
        {
            if (_wordDictionary.TryGetValue(key, out var hashSet))
                return hashSet.Add(value);

            _wordDictionary.Add(key, [value]);
        }

        return true;
    }

    public bool TryGetValue(TKey key, out HashSet<TValue>? values)
    {
        lock (_indexLock)
        {
            return _wordDictionary.TryGetValue(key, out values);
        }
    }

    public bool ContainsKey(TKey key)
    {
        lock (_indexLock)
        {
            return _wordDictionary.ContainsKey(key);
        }
    }
}