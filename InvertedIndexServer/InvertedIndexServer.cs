using System.Net;
using System.Text;
using System.Text.Json;
using InvertedIndexServer.InvertedIndex;

namespace InvertedIndexServer;

public class InvertedIndexServer
{
    private readonly ConcurrentInvertedIndex<string, string> _invertedIndex = new();
    private readonly InvertedIndexBuilder _invertedIndexBuilder = new();
    private readonly ThreadPool.ThreadPool _threadPool;
    private readonly HttpListener _httpListener;

    public InvertedIndexServer(int threadCount, string urlPrefix)
    {
        _threadPool = new ThreadPool.ThreadPool(threadCount);
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(urlPrefix);
    }

    public void Start()
    {
        _invertedIndexBuilder.BuildIndex(_invertedIndex);
        _threadPool.Run();
        _httpListener.Start();
        Console.WriteLine("Server started...");

        while (true)
        {
            var context = _httpListener.GetContext();
            _threadPool.AddTask(() => HandleRequest(context));
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
        _httpListener.Stop();
        _threadPool.Terminate();
        Console.WriteLine("Server stopped.");
    }
}
