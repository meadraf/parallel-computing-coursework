using System.Collections;

namespace InvertedIndexServer.InvertedIndex;

public class ConcurrentHashSet<T> : IEnumerable<T>
{
    public int Count => (int)_count;
    private LinkedList<T>[] _buckets;
    private ReaderWriterLockSlim[] _locks;
    private readonly ReaderWriterLockSlim _globalLock = new();
    private int _bucketCount;
    private long _count;
    private const double LoadFactorThreshold = 0.7;

    public ConcurrentHashSet(int initialBucketCount = 4)
    {
        _bucketCount = initialBucketCount;
        _buckets = new LinkedList<T>[initialBucketCount];
        _locks = new ReaderWriterLockSlim[initialBucketCount];

        for (var i = 0; i < initialBucketCount; i++)
        {
            _buckets[i] = [];
            _locks[i] = new ReaderWriterLockSlim();
        }
    }

    private int GetBucketIndex(T item) => Math.Abs(item.GetHashCode()) % _bucketCount;

    public bool Add(T item)
    {
        CheckAndExpand();

        var bucketIndex = GetBucketIndex(item);
        _locks[bucketIndex].EnterWriteLock();
        try
        {
            var bucket = _buckets[bucketIndex];
            if (bucket.Contains(item)) return false; // Елемент вже існує
            bucket.AddLast(item);
            Interlocked.Increment(ref _count);
            return true;
        }
        finally
        {
            _locks[bucketIndex].ExitWriteLock();
        }
    }

    public bool Remove(T item)
    {
        var bucketIndex = GetBucketIndex(item);
        _locks[bucketIndex].EnterWriteLock();
        try
        {
            var bucket = _buckets[bucketIndex];
            if (!bucket.Contains(item)) return false;
            bucket.Remove(item);
            Interlocked.Decrement(ref _count);
            return true;
        }
        finally
        {
            _locks[bucketIndex].ExitWriteLock();
        }
    }

    public bool Contains(T item)
    {
        var bucketIndex = GetBucketIndex(item);
        _locks[bucketIndex].EnterReadLock();
        try
        {
            var bucket = _buckets[bucketIndex];
            return bucket.Contains(item);
        }
        finally
        {
            _locks[bucketIndex].ExitReadLock();
        }
    }

    private void CheckAndExpand()
    {
        if ((double)Interlocked.Read(ref _count) / _bucketCount <= LoadFactorThreshold)return;

        _globalLock.EnterWriteLock();
        try
        {
            if ((double)Interlocked.Read(ref _count) / _bucketCount > LoadFactorThreshold)
            {
                ExpandAndRehash();
            }
        }
        finally
        {
            _globalLock.ExitWriteLock();
        }
    }

    private void ExpandAndRehash()
    {
        var newBucketCount = _bucketCount * 2;
        var newBuckets = new LinkedList<T>[newBucketCount];
        var newLocks = new ReaderWriterLockSlim[newBucketCount];

        for (var i = 0; i < newBucketCount; i++)
        {
            newBuckets[i] = [];
            newLocks[i] = new ReaderWriterLockSlim();
        }

        foreach (var bucket in _buckets)
        {
            foreach (var item in bucket)
            {
                var newBucketIndex = Math.Abs(item.GetHashCode()) % newBucketCount;
                newBuckets[newBucketIndex].AddLast(item);
            }
        }

        _buckets = newBuckets;
        _locks = newLocks;
        _bucketCount = newBucketCount;
    }

    public IEnumerator<T> GetEnumerator()
    {
        List<T> snapshot = [];
        
        foreach (var lockSlim in _locks)
        {
            lockSlim.EnterReadLock();
        }

        try
        {
            foreach (var bucket in _buckets)
            {
                snapshot.AddRange(bucket);
            }
        }
        finally
        {
            foreach (var lockSlim in _locks)
            {
                lockSlim.ExitReadLock();
            }
        }
        
        foreach (var pair in snapshot)
        {
            yield return pair;
        }
    }


    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
