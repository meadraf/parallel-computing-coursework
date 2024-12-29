const string baseUrl = "http://localhost:5003/";
var server = new InvertedIndexServer.InvertedIndexServer(3, baseUrl);

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