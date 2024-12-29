using System.Net;
using System.Text;
using System.Text.Json;
using InvertedIndexServer.InvertedIndex;

namespace InvertedIndexServer;

public class InvertedIndexServer
{
    private readonly ConcurrentInvertedIndex<string, string> _invertedIndex = new();
    private readonly InvertedIndexBuilder _invertedIndexBuilder = new();
    private readonly IndexDataWatcher _indexDataWatcher;
    private readonly ThreadPool.ThreadPool _threadPool;
    private readonly HttpListener _httpListener;
    private volatile bool _isRunning;

    private readonly ReaderWriterLockSlim _readerWriterLock = new();

    public InvertedIndexServer(int threadCount, string urlPrefix)
    {
        _threadPool = new ThreadPool.ThreadPool(threadCount);
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(urlPrefix);
        _indexDataWatcher = new IndexDataWatcher(_invertedIndex, _readerWriterLock);
    }

    public void Start()
    {
        _invertedIndexBuilder.BuildIndex(_invertedIndex);
        _indexDataWatcher.StartWatching();
        _threadPool.Run();
        _httpListener.Start();
        _isRunning = true;
        Console.WriteLine("Server started...");
        Console.WriteLine("Type 'stop' to shutdown the server");

        Task.Run(HandleConsoleInput);

        while (_isRunning)
        {
            var context = _httpListener.GetContext();
            _threadPool.AddTask(() => HandleRequest(context));
        }
    }

    private void HandleConsoleInput()
    {
        while (_isRunning)
        {
            var command = Console.ReadLine()?.Trim().ToLower();
            if (command != "stop") continue;
            Stop();
            break;
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;

            if (request.HttpMethod != "GET")
            {
                SendResponse(context, 405, "Method Not Allowed");
                return;
            }

            var query = request.QueryString["word"];
            if (string.IsNullOrWhiteSpace(query))
            {
                SendResponse(context, 400, "Bad Request: 'word' parameter is required.");
                return;
            }

            _readerWriterLock.EnterReadLock();
            try
            {
                if (_invertedIndex.TryGetValue(query.ToLowerInvariant(), out var fileNames))
                {
                    var responseJson = JsonSerializer.Serialize(fileNames);
                    SendResponse(context, 200, responseJson);
                }
                else
                {
                    SendResponse(context, 404, "Word not found in the index.");
                }
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling request: {ex.Message}");
            SendResponse(context, 500, "Internal Server Error");
        }
    }

    private static void SendResponse(HttpListenerContext context, int statusCode, string responseText)
    {
        context.Response.StatusCode = statusCode;
        var buffer = Encoding.UTF8.GetBytes(responseText);
        context.Response.ContentLength64 = buffer.Length;
        using var output = context.Response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
    }

    public void Stop()
    {
        _isRunning = false;
        _httpListener.Stop();
        _threadPool.Terminate();
        _indexDataWatcher.StopWatching();
        Console.WriteLine("Server stopped.");
    }
}