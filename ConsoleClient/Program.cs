using System.Net;
using System.Net.Http.Json;

public class ConsoleClient
{
    private string Server { get; set; }
    private readonly HttpClient _client = new();

    public async Task Connect()
    {
        _client.BaseAddress = new Uri("http://localhost:9090");
        HttpResponseMessage response = await _client.GetAsync("headertest");
        response.EnsureSuccessStatusCode();
        string jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"result: {jsonResponse}\n");
    }
    
    static void Main()
    {
        ConsoleClient client = new();

        Task connectTask = client.Connect();
        connectTask.GetAwaiter().GetResult();

    }
}