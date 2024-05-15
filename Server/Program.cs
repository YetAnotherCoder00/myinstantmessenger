// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace Server;

public class Server(string hostLocation)
{
    private SqliteConnection Connection { get; } = new SqliteConnection("Data Source=mim.db");
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
        Connection.CreateCommand();
        SqliteCommand command = new("CREATE TABLE accounts (username varchar(100), password varchar(100))");
        command.ExecuteNonQuery();
        command.CommandText = "CREATE TABLE messages (message varchar(1024), sender int, receiver int, " +
                              "FOREIGN KEY(sender) REFERENCES accounts(rowid), " +
                              "FOREIGN KEY(receiver) REFERENCES accounts(rowid))";
        command.ExecuteNonQuery();
        Connection.CloseAsync().GetAwaiter().GetResult();
    }

    public void GetUser()
    {
        Connection.OpenAsync().GetAwaiter();
        Connection.CreateCommand();
        SqliteCommand command = new("SELECT * FROM accounts");
        command.ExecuteNonQuery();
    }

    public async Task HandleConnections()
    {
        string lastMessage = string.Empty;
        string lastSentMessage = string.Empty;
        long lastUnixTime = 0;
        long lastSentUnixTime = 0;
        
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
                        if (request.HasEntityBody)
                        {
                            requestBody = new StreamReader(request.InputStream).ReadToEnd();
                        }
                        
                        string dataString = string.Empty;
                        // dataString += request.Headers.Get("cookie") + "\n";
                        dataString += requestBody + "\n";
                        lastMessage = requestBody;
                        lastUnixTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        // request.Headers.AllKeys.ToList().ForEach(elem => dataString += elem + "\n");
                        
                        data = Encoding.UTF8.GetBytes($"{dataString}\n{lastUnixTime}\n").ToList();

                        Console.WriteLine(dataString);
                        break;
                    case "/requestmessage":
                        if (lastSentMessage == lastMessage && lastUnixTime == lastSentUnixTime)
                        {
                            continue;
                        }
                        lastSentUnixTime = lastUnixTime;
                        lastSentMessage = lastMessage;
                        data = Encoding.UTF8.GetBytes($"{lastMessage}\n{lastUnixTime}").ToList();
                        Console.WriteLine(lastMessage);
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