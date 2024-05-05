// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text;

namespace Server;

public class Server
{
    private string HostLocation { get; }
    private HttpListener Listener { get; }

    public Server(string hostLocation)
    {
        HostLocation = hostLocation;
        Listener = new HttpListener();
    }

    public void Start()
    {
        Listener.Prefixes.Add(HostLocation);
        Listener.Start();
        
        
    }

    public async Task HandleConnections()
    {
        while (true)
        {
            HttpListenerContext context = await Listener.GetContextAsync();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/shutdown")
            {
                break;
            }

            byte[] data = Encoding.UTF8.GetBytes($"{request.Url}");
            response.ContentType = "text/html";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = data.LongLength;

            await response.OutputStream.WriteAsync(data, 0, data.Length);
            response.Close();
        }
    }

    static void Main()
    {
        return;
    }
}