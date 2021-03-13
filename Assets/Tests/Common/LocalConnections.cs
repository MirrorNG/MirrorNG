namespace Mirage.Tests
{

    public static class LocalConnections
    {
        public static (NetworkPlayer, NetworkPlayer) PipedConnections()
        {
            (IConnection c1, IConnection c2) = PipeConnection.CreatePipe();
            var toServer = new NetworkPlayer(c2);
            var toClient = new NetworkPlayer(c1);

            return (toServer, toClient);
        }

    }
}
