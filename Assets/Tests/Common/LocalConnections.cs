using NSubstitute;

namespace Mirage.Tests
{
    public static class LocalConnections
    {
        public static (NetworkPlayer, NetworkPlayer) PipedConnections(IMessageHandler serverHandler, IMessageHandler clientHandler)
        {
            (IConnection c1, IConnection c2) = PipeConnection.CreatePipe();
            var toServer = new NetworkPlayer(c2, clientHandler);
            var toClient = new NetworkPlayer(c1, serverHandler);

            return (toServer, toClient);
        }

        public static (NetworkPlayer, NetworkPlayer) PipedConnections()
        {
            return PipedConnections(Substitute.For<IMessageHandler>(), Substitute.For<IMessageHandler>());
        }
    }
}
