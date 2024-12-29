namespace InvertedIndexServer;

public class Configuration
{
    public string IndexDataPath => _projectDirectory + "/IndexData";

    private readonly string _projectDirectory =
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../.."));
}