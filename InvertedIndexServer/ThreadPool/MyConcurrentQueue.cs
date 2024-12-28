namespace InvertedIndexServer.ThreadPool;

public class MyConcurrentQueue<T>
{
    private Queue<T> _queue = new();
    private readonly object _queueLock = new();

    public int Count
    {
        get
        {
            lock (_queueLock)
            {
                return _queue.Count;
            }
        }
    }

    public void Enqueue(T value)
    {
        lock (_queueLock)
        {
            _queue.Enqueue(value);
        }
    }

    public T Dequeue()
    {
        lock (_queueLock)
        {
            return _queue.Dequeue();
        }
    }

    public void Clear()
    {
        lock (_queueLock)
        {
            _queue = new Queue<T>();
        }
    }
}