using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebSocketServer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Listen over TCP for a handshake request
            // Upgrade request to WebSocket connection

            // assigning an ipaddress and a port to our server
            IPAddress serverIPAddress = IPAddress.Parse("127.0.0.1");
            int portNumber = 4444;
            IPEndPoint ipEndPoint = new(serverIPAddress, portNumber);

            // TCP Listener for incoming requests over the above IP Address
            TcpListener tcpListener = new TcpListener(ipEndPoint);
            try
            {
                tcpListener.Start();
                // Ready to listen, accept and process requests
                using TcpClient handler = await tcpListener.AcceptTcpClientAsync();
                await using NetworkStream stream = handler.GetStream();
                // Convert received bytes into string
                int bufferLength = 1024;
                byte[] buffer = new byte[bufferLength];
                stream.Read(buffer, 0, bufferLength);
                string data = Encoding.UTF8.GetString(buffer);
                // Print received data to console
                Console.WriteLine(data);
            }
            finally
            {
                tcpListener.Stop();
            }
        }
    }
}
