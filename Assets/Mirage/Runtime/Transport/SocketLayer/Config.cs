namespace Mirage.SocketLayer
{
    public struct Config
    {
        // server

        public int MaxConnections;


        // connections

        public float ConnectAttemptInterval;
        public int MaxConnectAttempts;

        public float KeepAliveInterval;
        public float DisconnectTimeout;
    }
}
