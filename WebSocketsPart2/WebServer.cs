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

                using TcpClient tcpHandler = await _tcpListener.AcceptTcpClientAsync();
                // Spawn new tasks here to allow asynchronous request handling
                await using NetworkStream stream = tcpHandler.GetStream();
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

                    // Factory pattern to maintain our sanity :)
                    IEndpointHandler handler = EndpointHandlerFactory.Create(stream, endpointDetails);
                    try
                    {
                        WebServerResponse response = handler.ProcessRequest();
                        if (response != null)
                            WriteResponse(stream, response.Status, response.Data);
                    }
                    catch (Exception ex)
                    {
                        WriteResponse(stream, "HTTP/1.1 500 Internal Server Error", ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    stream.Close();
                    tcpHandler.Close();
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
        return new Endpoint { HttpMethod = method, Route = route, Data = data };
    }
}

class Endpoint
{
    public MyHttpMethods HttpMethod { get; set; }
    public string Route { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}

class WebServerResponse
{
    public string Status { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}