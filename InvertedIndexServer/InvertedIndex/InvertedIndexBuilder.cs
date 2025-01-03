using System.Diagnostics;
using System.Text.RegularExpressions;

namespace InvertedIndexServer.InvertedIndex;

public partial class InvertedIndexBuilder
{
    private readonly Configuration _configuration = new();
    private readonly List<Thread> _threads = [];

    public void BuildIndex(ConcurrentInvertedIndex<string, string> invertedIndex, int threadCount)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        if (!Directory.Exists(_configuration.IndexDataPath))
        {
            Console.WriteLine("Folder does not exist.");
            return;
        }

        var txtFiles = Directory.GetFiles(_configuration.IndexDataPath, "*.txt");

        for (var i = 0; i < threadCount; i++)
        {
            var startingIndex = i;
            _threads.Add(new Thread(() => IndexFiles(invertedIndex, txtFiles, startingIndex, threadCount)));
        }
        
        foreach (var thread in _threads)
        {
            thread.Start();
        }

        foreach (var thread in _threads)
        {
            thread.Join();
        }
        
        stopwatch.Stop();
        Console.WriteLine($"Indexing time: {stopwatch.ElapsedMilliseconds}");
        Console.WriteLine("Indexing complete.");
    }

    private static void IndexFiles(ConcurrentInvertedIndex<string, string> invertedIndex, string[] files, int start,
        int step)
    {
        for (var i = start; i < files.Length; i += step)
        {
            var filePath = files[i];
            try
            {
                var fileContent = File.ReadAllText(filePath);

                fileContent = HtmlRegex().Replace(fileContent, string.Empty);

                var words = PunctuationRegex().Matches(fileContent)
                    .Select(match => match.Value.ToLowerInvariant());

                var fileName = Path.GetFileName(filePath);

                foreach (var word in words)
                {
                    invertedIndex.TryAdd(word, fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlRegex();

    [GeneratedRegex(@"\b\w+\b")]
    private static partial Regex PunctuationRegex();
}