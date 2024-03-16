
namespace WebSocketServer;

internal class EndpointHandlerFactory
{
    internal static IEndpointHandler Create(System.Net.Sockets.NetworkStream stream, Endpoint endpointDetails)
    {
        return endpointDetails.Route switch
        {
            "/" => new DefaultEndpointHandler(endpointDetails),
            "/socket" => new SocketEndpointHandler(stream, endpointDetails),
            _ => throw new InvalidDataException("Undefined route.")
        };
    }
}
