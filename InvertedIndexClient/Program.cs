using InvertedIndexClient;

const string baseUrl = "http://localhost:5003/";

using var client = new Client(baseUrl);

while (true)
{
    Console.WriteLine("Enter a word to search (or 'exit' to quit):");
    var word = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(word) || word.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    await client.QueryWordAsync(word);
}

Console.WriteLine("Client terminated.");