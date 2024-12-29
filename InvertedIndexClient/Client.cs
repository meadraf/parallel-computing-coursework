namespace InvertedIndexClient;

using System;
using System.Net.Http;
using System.Threading.Tasks;

public class Client(string baseUrl) : IDisposable
{
    private readonly HttpClient _httpClient = new() { BaseAddress = new Uri(baseUrl) };

    public async Task QueryWordAsync(string word)
    {
        try
        {
            // Send GET request with the word as a query parameter
            var response = await _httpClient.GetAsync($"?word={Uri.EscapeDataString(word)}");

            if (response.IsSuccessStatusCode)
            {
                var fileNames = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Files containing the word '{word}': {fileNames}");
            }
            else
            {
                Console.WriteLine($"Error {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

