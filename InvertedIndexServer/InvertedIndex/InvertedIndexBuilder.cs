using System.Text.RegularExpressions;

namespace InvertedIndexServer.InvertedIndex;

public partial class InvertedIndexBuilder
{
    private readonly Configuration _configuration = new();
    
    public void BuildIndex(ConcurrentInvertedIndex<string, string> invertedIndex)
    {
        if (!Directory.Exists(_configuration.IndexDataPath))
        {
            Console.WriteLine("Folder does not exist.");
            return;
        }

        var txtFiles = Directory.GetFiles(_configuration.IndexDataPath, "*.txt");

        foreach (var filePath in txtFiles)
        {
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

                Console.WriteLine($"Indexed file: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }
        
        Console.WriteLine("Indexing complete.");
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlRegex();

    [GeneratedRegex(@"\b\w+\b")]
    private static partial Regex PunctuationRegex();
}