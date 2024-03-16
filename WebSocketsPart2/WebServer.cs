using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebSocketServer;

internal class WebServer
{
    readonly IPEndPoint _ipEndPoint;
    readonly TcpListener _tcpListener;
    readonly Endpoint[] _validEndpoints;

    public WebServer(string ipAddress, int port, Endpoint[] validEndpoints)
    {
        // assigning an ipaddress and a port to our server
        _ipEndPoint = new(IPAddress.Parse(ipAddress), port);
        // TCP Listener for incoming requests over the above IP Address
        _tcpListener = new(_ipEndPoint);
        _validEndpoints = validEndpoints;
    }

    internal async Task StartAsync()
    {
        try
        {
            _tcpListener.Start();
            Console.WriteLine("Tcp Server up and running ...");
            // Started listening and ready to accept and process requests
            while (true)
            {
                Console.WriteLine($"== REQUEST START: {DateTime.UtcNow} =======================================");

                using TcpClient handler = await _tcpListener.AcceptTcpClientAsync();
                // Spawn new tasks here to allow asynchronous request handling
                await using NetworkStream stream = handler.GetStream();
                try
                {
                    // Convert received bytes into string
                    int bufferLength = 1024; // Not a magic number .. increase or decrease as per your need
                    byte[] buffer = new byte[bufferLength];
                    stream.Read(buffer, 0, bufferLength);
                    string data = Encoding.UTF8.GetString(buffer);
                    Console.WriteLine(data);
                    // Extract Http Method and Route
                    Endpoint endpointDetails = ExtractMethodAndRoute(data);
                    // Verify Requested Route
                    if (!VerifyRoutes(endpointDetails))
                    {
                        WriteResponse(stream, "HTTP/1.1 404 Not Found");
                        continue;
                    }
                    // Extract Headers
                    Dictionary<string, string> headers = ExtractHeaders(data);
                    // Extract Body
                    string body = ExtractBody(data);

                    // Write a simple OK response with some test message
                    WriteResponse(stream, "HTTP/1.0 200 OK", "200 OK");

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                Console.WriteLine($"== REQUEST END: {DateTime.UtcNow} =======================================");
            }
        }
        finally
        {
            _tcpListener.Stop();
        }

    }

    private bool VerifyRoutes(Endpoint endpointDetails)
        => _validEndpoints.Any(x => x.Route == endpointDetails.Route && x.HttpMethod == endpointDetails.HttpMethod);

    private static void WriteResponse(NetworkStream stream, string statusDetails, string content = "")
    {
        stream.Write(Encoding.UTF8.GetBytes(statusDetails));
        stream.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
        stream.Write(Encoding.UTF8.GetBytes("Content-Type: text/plain; charset=UTF-8"));
        stream.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
        stream.Write(Encoding.UTF8.GetBytes("Content-Length: " + content.Length));
        stream.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
        stream.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
        stream.Write(Encoding.UTF8.GetBytes(content));
    }

    private static Endpoint ExtractMethodAndRoute(string data)
    {
        string firstLine = data.Substring(0, data.IndexOf('\n'));
        string[] tokens = firstLine.Split(' ');
        MyHttpMethods method = tokens[0] switch
        {
            "GET" => MyHttpMethods.GET,
            "POST" => MyHttpMethods.POST,
            "PUT" => MyHttpMethods.PUT,
            "PATCH" => MyHttpMethods.PATCH,
            _ => MyHttpMethods.GET,
        };
        string route = tokens[1];
        return new Endpoint { HttpMethod = method, Route = route };
    }

    private static string ExtractBody(string data) => data[(data.IndexOf($"{Environment.NewLine}{Environment.NewLine}") + 1)..];

    private static Dictionary<string, string> ExtractHeaders(string data)
    {
        int headerStartIndex = data.IndexOf('\n') + 1;
        int headerEndIndex = data.IndexOf($"{Environment.NewLine}{Environment.NewLine}");
        string headers = data[headerStartIndex..headerEndIndex]; // Range Operator 🔥🔥🔥
        var headersArray = headers.Split('\n');
        Dictionary<string, string> result = [];
        foreach (var headerEntry in headersArray)
        {
            string[] tokens = headerEntry.Split(' ');
            result.Add(tokens[0].Replace(":", ""), tokens[1]);
        }
        return result;
    }
}

class Endpoint
{
    public MyHttpMethods HttpMethod { get; set; }
    public string Route { get; set; } = string.Empty;
}