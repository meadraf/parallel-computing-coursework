using System.Text.RegularExpressions;

namespace InvertedIndexServer.InvertedIndex;

public partial class IndexDataWatcher
{
    private readonly Configuration _configuration = new();

    private readonly ConcurrentInvertedIndex<string, string> _invertedIndex;
    private readonly FileSystemWatcher _fileSystemWatcher;

    private readonly ReaderWriterLockSlim _readerWriterLock;

    public IndexDataWatcher(ConcurrentInvertedIndex<string, string> invertedIndex,
        ReaderWriterLockSlim readerWriterLock)
    {
        _invertedIndex = invertedIndex;
        _readerWriterLock = readerWriterLock;
        _fileSystemWatcher = new FileSystemWatcher(_configuration.IndexDataPath, "*.txt");
        _fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        _fileSystemWatcher.Created += OnFileCreated;
        _fileSystemWatcher.Deleted += OnFileDeleted;
    }

    public void StartWatching()
    {
        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    public void StopWatching()
    {
        _fileSystemWatcher.EnableRaisingEvents = false;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        var fileContent = File.ReadAllText(e.FullPath);
        fileContent = HtmlRegex().Replace(fileContent, string.Empty);
        var words = PunctuationRegex().Matches(fileContent)
            .Select(match => match.Value.ToLowerInvariant());
        var fileName = Path.GetFileName(e.FullPath);
        
        _readerWriterLock.EnterWriteLock();
        try
        {
            foreach (var word in words)
            {
                _invertedIndex.TryAdd(word, fileName);
            }

            Console.WriteLine($"File added and indexed: {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error indexing new file {e.FullPath}: {ex.Message}");
        }
        finally
        {
            _readerWriterLock.ExitWriteLock();
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        var fileName = Path.GetFileName(e.FullPath);
        
        _readerWriterLock.EnterWriteLock();
        try
        {
            foreach (var key in _invertedIndex)
            {
                _invertedIndex.TryRemoveValue(key, fileName);
            }

            Console.WriteLine($"File removed from index: {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing file {e.FullPath} from index: {ex.Message}");
        }
        finally
        {
            _readerWriterLock.ExitWriteLock();
        }
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlRegex();

    [GeneratedRegex(@"\b\w+\b")]
    private static partial Regex PunctuationRegex();
}