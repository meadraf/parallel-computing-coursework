using InvertedIndexServer.CancellationToken;

namespace InvertedIndexServer.ThreadPool;

public class ThreadPool
{
    public bool IsRunning { get; private set; }

    private readonly List<Thread> _threads = [];
    private readonly MyConcurrentQueue<Action> _queue = new();
    private readonly MyCancellationTokenSource _cancellationTokenSource = new();
    private readonly MyCancellationToken _token;

    private readonly object _taskWaiter = new();
    private bool _isTerminated;
    private bool _isPaused;

    public ThreadPool(int threadCount)
    {
        _token = _cancellationTokenSource.Token;
        for (var i = 0; i < threadCount; i++)
        {
            _threads.Add(new Thread(Routine));
        }
    }

    public void AddTask(Action task)
    {
        lock (_taskWaiter)
        {
            if (_isTerminated)
            {
                return;
            }

            _queue.Enqueue(task);
            Monitor.Pulse(_taskWaiter);
        }
    }

    public void Run()
    {
        foreach (var thread in _threads)
        {
            thread.Start();
        }

        IsRunning = true;
    }

    public void Terminate()
    {
        lock (_taskWaiter)
        {
            _isTerminated = true;
            Monitor.PulseAll(_taskWaiter);
        }

        lock (_threads)
        {
            foreach (var thread in _threads)
            {
                thread.Join();
            }

            _threads.Clear();
        }

        _queue.Clear();
        _isTerminated = false;
        IsRunning = false;
    }

    public void Kill()
    {
        lock (_taskWaiter)
        {
            _isTerminated = true;
            _queue.Clear();
            Monitor.PulseAll(_taskWaiter);
        }

        _cancellationTokenSource.Cancel();

        lock (_threads)
        {
            foreach (var thread in _threads)
            {
                thread.Join();
            }

            _threads.Clear();
        }

        _cancellationTokenSource.Reset();
        _isTerminated = false;
        IsRunning = false;
    }

    public void Pause()
    {
        lock (_taskWaiter)
        {
            _isPaused = true;
        }
    }

    public void Resume()
    {
        lock (_taskWaiter)
        {
            _isPaused = false;
            Monitor.PulseAll(_taskWaiter);
        }
    }

    private void Routine()
    {
        while (true)
        {
            Action task;

            lock (_taskWaiter)
            {
                while (!_isTerminated && (_queue.Count == 0 || _isPaused))
                {
                    Monitor.Wait(_taskWaiter);
                }

                if (_isTerminated && _queue.Count == 0) return;
                task = _queue.Dequeue();
            }

            task.Invoke();
        }
    }
}