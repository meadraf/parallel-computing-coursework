using System.Threading.Channels;
using InvertedIndexServer.InvertedIndex;
using InvertedIndexServer.ThreadPool;

ConcurrentInvertedIndex<string, string> index = new ConcurrentInvertedIndex<string, string>();
Dictionary<int, int> dictionary = new Dictionary<int, int>();

var invertedIndexBuilder = new InvertedIndexBuilder();
invertedIndexBuilder.BuildIndex(index);

Console.WriteLine(index.TryGetValue("house", out var values));

foreach (var v in values)
{
    Console.WriteLine(v);
}




