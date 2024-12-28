using System.Threading.Channels;
using InvertedIndexServer.InvertedIndex;
using InvertedIndexServer.ThreadPool;

ConcurrentInvertedIndex<string, string> index = new ConcurrentInvertedIndex<string, string>();
Dictionary<int, int> dictionary = new Dictionary<int, int>();





