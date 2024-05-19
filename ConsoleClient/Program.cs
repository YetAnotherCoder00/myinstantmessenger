public class ConsoleClient
{
    private HttpClient _client = new();

    public async Task Connect()
    {
        _client.BaseAddress = new Uri("http://localhost:9090");
        string jsonResponse = "error";
        try
        {
            HttpResponseMessage response = await _client.GetAsync("ping");
            jsonResponse = await response.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            return;
        }
        if (jsonResponse != "pong")
        {
            Console.WriteLine($"error: couldn't connect to {_client.BaseAddress}");
            throw new HttpRequestException("server found, but incorrect response");
        }
        Console.WriteLine($"result: {jsonResponse}\n");
        _client.DefaultRequestHeaders.Add("auth", "authenticated");
    }

    public async void SendMessage(string message)
    {
        // string message = Console.ReadLine() ?? string.Empty;
        try
        {
            _client.DefaultRequestHeaders.Date = DateTimeOffset.Now;
            HttpResponseMessage response = await _client.PostAsync("sendmessage", new StringContent(message));
            response.EnsureSuccessStatusCode();
            string jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine(jsonResponse);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public async void RecieveMessage()
    {
        _client.DefaultRequestHeaders.Date = DateTimeOffset.Now;
        HttpResponseMessage response = await _client.PostAsync("requestmessage", new StringContent("directMessage1"));
        response.EnsureSuccessStatusCode();
        string jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine(jsonResponse);

    }
    
    static void Main()
    {
        Console.WriteLine("1: sender, 2: reciever, 3: mix");
        string input = Console.ReadLine() ?? string.Empty;
        // string input = "1";
        ConsoleClient client = new();

        try
        {
            Task connectTask = client.Connect();
            // client.SendMessage("test");
            connectTask.GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            Console.WriteLine("error");
            return;
        }
        
        int messageCount = 0;

        if (input == "1")
        {
            while (true)
            {
                Thread.Sleep(1000);
                try
                {
                    client.SendMessage($"Message Nr. {messageCount}");
                    messageCount += 1;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        } 
        if (input == "2")
        {
            while (true)
            {
                Thread.Sleep(1000);
                try
                {
                    client.RecieveMessage();
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
        
    }
}