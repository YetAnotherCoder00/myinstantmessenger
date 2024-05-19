// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Server;

struct Message
{
    public string Text { get; set; }
    public DateTimeOffset Time { get; set; }
}

public class Server(string hostLocation)
{
    private SqliteConnection Connection { get; } = new("Data Source=mim.db");
    private string HostLocation { get; } = hostLocation;
    private HttpListener Listener { get; } = new();

    public void Start()
    {
        Listener.Prefixes.Add(HostLocation);
        Listener.Start();
    }

    public void SetupOnce()
    {
        // ONLY RUN THIS ONCE
        Connection.OpenAsync().GetAwaiter().GetResult();
        SqliteCommand command = Connection.CreateCommand();
        command.CommandText = "CREATE TABLE accounts (username varchar(100), password varchar(100))";
        command.ExecuteNonQuery();
        // command.CommandText = "CREATE TABLE messages (message varchar(1024), sender int, receiver int, " +
        //                       "FOREIGN KEY(sender) REFERENCES accounts(rowid), " +
        //                       "FOREIGN KEY(receiver) REFERENCES accounts(rowid))";
        command.CommandText = "CREATE TABLE testMessages (message varchar(1024), unixTime integer)";
        command.ExecuteNonQuery();
        Connection.CloseAsync().GetAwaiter().GetResult();
    }

    public void GetUser()
    {
        Connection.OpenAsync().GetAwaiter();
        SqliteCommand command = Connection.CreateCommand();
        command.CommandText = "SELECT * FROM accounts";
        command.ExecuteNonQuery();
    }

    public async Task HandleConnections()
    {
        string lastMessage = string.Empty;
        long lastUnixTime = 0;
        long lastSentUnixTime = 0;

        DateTimeOffset lastClientSync = DateTimeOffset.MinValue;
        
        while (true)
        {
            string requestBody = string.Empty;
            List<byte> data = new List<byte>();
            HttpListenerContext context = await Listener.GetContextAsync();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            
            if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/shutdown")
            {
                break;
            }

            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/ping")
            {
                data = "pong"u8.ToArray().ToList();
            }

            if (request.HttpMethod == "POST")
            {
                switch (request.Url?.AbsolutePath)
                {
                    case "/test":
                        data = "test1"u8.ToArray().ToList();
                        break;
                    case "/sendmessage":
                        // client sends message to server
                        if (request.HasEntityBody)
                        {
                            requestBody = new StreamReader(request.InputStream).ReadToEnd();
                        }
                        
                        string dataString = string.Empty;
                        dataString += requestBody + "\n";
                        lastMessage = requestBody;
                        
                        data = Encoding.UTF8.GetBytes($"{dataString}\n{request.Headers.Get("Date")}\n").ToList();
                        
                        SaveMessage(dataString, request.Headers.Get("Date") ?? string.Empty);
                        
                        Console.WriteLine(dataString);
                        break;
                    case "/requestmessage":
                        // client requests message from server
                        
                        var clientAccess = DateTimeOffset.Parse(request.Headers.Get("Date"));

                        DateTimeOffset latestMessage = GetLatestMessageTime();
                        
                        if (latestMessage > lastClientSync)
                        {
                            lastClientSync = clientAccess;
                            List<Message> messages = FetchMessages();
                            // check database for messages between last client access and current access
                            data = new List<byte>();
                            messages.ForEach(elem =>
                            {
                                data.AddRange(Encoding.UTF8.GetBytes($"{elem.Text}\\@\\{elem.Time}"));
                            });
                            lastClientSync = clientAccess;
                        }
                        
                        break;
                    case "/createaccount":
                        string username = request.Headers.Get("username") ?? string.Empty;
                        string password = request.Headers.Get("password") ?? string.Empty;

                        break;
                    default:
                        data = Encoding.UTF8.GetBytes(request.Url?.AbsolutePath ?? string.Empty).ToList();
                        break;
                }
            }

            
            response.ContentType = "text/html";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = data.Count;

            await response.OutputStream.WriteAsync(data.ToArray(), 0, data.Count);
            response.Close();
        }
    }

    List<Message> FetchMessages()
    {
        List<Message> messages = new();
        Connection.OpenAsync().GetAwaiter().GetResult();
        SqliteCommand command = Connection.CreateCommand();
        command.CommandText = "SELECT * FROM testMessages";
        SqliteDataReader messageReader = command.ExecuteReaderAsync().GetAwaiter().GetResult();
        while (messageReader.ReadAsync().GetAwaiter().GetResult())
        {
            // create struct or something for message and time stamp
            Message message = new();
            message.Text = messageReader.GetString(0);
            message.Time = DateTimeOffset.FromUnixTimeMilliseconds(messageReader.GetInt64(1));
            messages.Add(message);
        }

        return messages;
    }

    void SaveMessage(string data, string time)
    {
        DateTimeOffset timeOffset = DateTimeOffset.Parse(time);
        long unixTime = timeOffset.ToUnixTimeMilliseconds();
        Connection.OpenAsync().GetAwaiter().GetResult();
        SqliteCommand command = Connection.CreateCommand();
        command.CommandText = $"INSERT INTO testMessages VALUES (\"{data}\", {unixTime})";
        command.ExecuteNonQuery();
    }

    DateTimeOffset GetLatestMessageTime()
    {
        Connection.OpenAsync().GetAwaiter().GetResult();
        SqliteCommand command = Connection.CreateCommand();
        command.CommandText = $"SELECT unixTime FROM testMessages ORDER BY unixTime LIMIT 1";
        object? result = command.ExecuteScalar();
        if (result == null || result.ToString() == string.Empty)
        {
            return DateTimeOffset.MinValue;
        }

        long time = long.Parse(result.ToString());
        DateTimeOffset latestMessageTime = DateTimeOffset.FromUnixTimeMilliseconds(time);
        return latestMessageTime;
    }

    static void Main()
    {
        Server server = new("http://localhost:9090/");
        List<string> file = Directory.EnumerateFiles(".", "mim.db", 
            SearchOption.TopDirectoryOnly).ToList();

        if (!file.Exists(elem => elem.Contains("mim")))
        {
            server.SetupOnce();
        }
        server.Start();
        server.HandleConnections().GetAwaiter().GetResult();
    }
}