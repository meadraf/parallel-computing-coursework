using System.Collections;

namespace InvertedIndexServer.InvertedIndex;

using System;
using System.Collections.Generic;
using System.Threading;

public class ConcurrentHashTable<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    private LinkedList<KeyValuePair<TKey, TValue>>[] _buckets;
    private ReaderWriterLockSlim[] _locks;
    private readonly ReaderWriterLockSlim _globalLock = new();
    private int _bucketCount;
    private long _count;
    private const double LoadFactorThreshold = 0.7;

    public ConcurrentHashTable(int initialBucketCount = 16)
    {
        _bucketCount = initialBucketCount;
        _buckets = new LinkedList<KeyValuePair<TKey, TValue>>[initialBucketCount];
        _locks = new ReaderWriterLockSlim[initialBucketCount];

        for (var i = 0; i < initialBucketCount; i++)
        {
            _buckets[i] = [];
            _locks[i] = new ReaderWriterLockSlim();
        }
    }

    private int GetBucketIndex(TKey key) => Math.Abs(key.GetHashCode()) % _bucketCount;

    public void AddOrUpdate(TKey key, TValue value)
    {
        //CheckAndExpand();

        var bucketIndex = GetBucketIndex(key);
        _locks[bucketIndex].EnterWriteLock();
        try
        {
            var bucket = _buckets[bucketIndex];
            var node = FindNode(bucket, key);
            if (node != null)
            {
                bucket.Remove(node);
            }
            else
            {
                Interlocked.Increment(ref _count);
            }

            bucket.AddLast(new KeyValuePair<TKey, TValue>(key, value));
        }
        finally
        {
            if(_locks[bucketIndex].IsWriteLockHeld)
                _locks[bucketIndex].ExitWriteLock();
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        var bucketIndex = GetBucketIndex(key);
        _locks[bucketIndex].EnterReadLock();
        try
        {
            var bucket = _buckets[bucketIndex];
            var node = FindNode(bucket, key);
            if (node != null)
            {
                value = node.Value.Value;
                return true;
            }

            value = default;
            return false;
        }
        finally
        {
            if(_locks[bucketIndex].IsReadLockHeld)
                _locks[bucketIndex].ExitReadLock();
        }
    }

    public bool Contains(TKey key)
    {
        var bucketIndex = GetBucketIndex(key);
        _locks[bucketIndex].EnterReadLock();
        try
        {
            var bucket = _buckets[bucketIndex];
            var node = FindNode(bucket, key);
            return node != null;
        }
        finally
        {
            if(_locks[bucketIndex].IsReadLockHeld)
                _locks[bucketIndex].ExitReadLock();
        }
    }


    public bool Remove(TKey key)
    {
        var bucketIndex = GetBucketIndex(key);
        _locks[bucketIndex].EnterWriteLock();
        try
        {
            var bucket = _buckets[bucketIndex];
            var node = FindNode(bucket, key);
            if (node == null) return false;
            bucket.Remove(node);
            Interlocked.Decrement(ref _count);
            return true;
        }
        finally
        {
            if(_locks[bucketIndex].IsWriteLockHeld)
                _locks[bucketIndex].ExitWriteLock();
        }
    }

    private static LinkedListNode<KeyValuePair<TKey, TValue>> FindNode(LinkedList<KeyValuePair<TKey, TValue>> bucket,
        TKey key)
    {
        return (from node in bucket where EqualityComparer<TKey>.Default.Equals(node.Key, key) select bucket.Find(node))
            .FirstOrDefault()!;
    }

    private void CheckAndExpand()
    {
        if ((double) Interlocked.Read(ref _count) / _bucketCount <= LoadFactorThreshold) return;

        _globalLock.EnterWriteLock();
        try
        {
            if ((double) Interlocked.Read(ref _count) / _bucketCount > LoadFactorThreshold)
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
        var newBuckets = new LinkedList<KeyValuePair<TKey, TValue>>[newBucketCount];
        var newLocks = new ReaderWriterLockSlim[newBucketCount];

        for (var i = 0; i < newBucketCount; i++)
        {
            newBuckets[i] = [];
            newLocks[i] = new ReaderWriterLockSlim();
        }

        foreach (var bucket in _buckets)
        {
            foreach (var pair in bucket)
            {
                var newBucketIndex = Math.Abs(pair.Key.GetHashCode()) % newBucketCount;
                newBuckets[newBucketIndex].AddLast(pair);
            }
        }

        _buckets = newBuckets;
        _bucketCount = newBucketCount;
        _locks = newLocks;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        List<KeyValuePair<TKey, TValue>> snapshot = [];
        
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