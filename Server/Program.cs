// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text;

namespace Server;

public class Server(string hostLocation)
{
    private string HostLocation { get; } = hostLocation;
    private HttpListener Listener { get; } = new();

    public void Start()
    {
        Listener.Prefixes.Add(HostLocation);
        Listener.Start();
    }

    public async Task HandleConnections()
    {
        while (true)
        {
            List<byte> data = new List<byte>();
            HttpListenerContext context = await Listener.GetContextAsync();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            
            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/shutdown")
            {
                break;
            }

            if (request.HttpMethod == "GET")
            {
                switch (request.Url?.AbsolutePath)
                {
                    case "/test":
                        data = "test1"u8.ToArray().ToList();
                        break;
                    case "/headertest":
                        data = Encoding.UTF8.GetBytes(request.Headers.ToString()).ToList();
                        break;
                    default:
                        data = Encoding.UTF8.GetBytes(request.Url?.AbsolutePath).ToList();
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
        server.Start();
        server.HandleConnections().GetAwaiter().GetResult();
    }
}