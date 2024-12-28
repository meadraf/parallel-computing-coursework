namespace InvertedIndexServer.CancellationToken;

public class MyCancellationTokenSource
{
    public MyCancellationToken Token { get; }

    public bool IsCancellationRequested { get; private set; }
    private readonly object _lock = new();

    public MyCancellationTokenSource()
    {
        Token = new MyCancellationToken(this, _lock);
    }

    public void Cancel()
    {
        lock (_lock)
        {
            IsCancellationRequested = true;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            IsCancellationRequested = false;
        }
    }
}

public class MyCancellationToken(MyCancellationTokenSource source, object tokenLock)
{
    public bool IsCancellationRequested => IsCancelled();

    private bool IsCancelled()
    {
        lock (tokenLock)
        {
            return source.IsCancellationRequested;
        }
    }
}