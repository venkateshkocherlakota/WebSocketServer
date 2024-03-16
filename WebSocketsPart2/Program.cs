using System.Net.Sockets;
using System.Text;

namespace WebSocketServer
{
    enum MyHttpMethods
    {
        GET,
        POST,
        PUT,
        PATCH
    }



    internal class Program
    {


        static async Task Main(string[] args)
        {

            WebServer server = new("127.0.0.1", 4444, [
                new Endpoint { HttpMethod = MyHttpMethods.GET, Route = "/" },
                new Endpoint { HttpMethod = MyHttpMethods.GET, Route = "/socket" },
            ]);
            await server.StartAsync();
        }
    }
}
