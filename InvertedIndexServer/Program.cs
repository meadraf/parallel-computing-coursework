using System.Threading.Channels;
using InvertedIndexServer.InvertedIndex;
using InvertedIndexServer.ThreadPool;

var server = new InvertedIndexServer.InvertedIndexServer(4, "http://localhost:5000/");

try
{
    server.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Server error: {ex.Message}");
}
finally
{
    server.Stop();
}




