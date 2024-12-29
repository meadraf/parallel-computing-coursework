using System.Collections;

namespace InvertedIndexServer.InvertedIndex;

public class ConcurrentInvertedIndex<TKey, TValue> : IInvertedIndex<TKey, TValue>,
    IEnumerable<TKey> where TKey : notnull
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

    public bool TryRemoveValue(TKey key, TValue value)
    {
        lock (_indexLock)
        {
            if (!_wordDictionary.TryGetValue(key, out var values)) return false;
            if (!values.Remove(value)) return false;
            if (values.Count == 0)
                _wordDictionary.Remove(key);

            return true;
        }
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

    public IEnumerator<TKey> GetEnumerator()
    {
        List<TKey> keysSnapshot;
        lock (_indexLock)
        {
            keysSnapshot = [.._wordDictionary.Keys];
        }

        return keysSnapshot.GetEnumerator();
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}