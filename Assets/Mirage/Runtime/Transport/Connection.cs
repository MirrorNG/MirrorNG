using System;
using System.Net;
using UnityEngine;

namespace Mirage.SocketLayer
{

    public enum ConnectionState
    {
        Created = 1,
        Connecting = 2,
        Connected = 3,

        // ..

        Disconnected = 9,
        Destroyed = 10,
    }

    public sealed class Connection
    {
        static readonly ILogger logger = LogFactory.GetLogger<Connection>();

        public ConnectionState State { get; private set; }
        public readonly EndPoint EndPoint;
        private readonly Config config;

        public Connection(EndPoint endPoint, Config config)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            this.config = config;

            State = ConnectionState.Created;
        }

        public float LastRecvPacketTime { get; internal set; }

        public void ChangeState(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Connected:
                    logger.Assert(State == ConnectionState.Created || State == ConnectionState.Connecting);
                    break;

                case ConnectionState.Connecting:
                    logger.Assert(State == ConnectionState.Created);
                    break;

                case ConnectionState.Disconnected:
                    logger.Assert(State == ConnectionState.Connected);
                    break;
            }

            if (logger.LogEnabled()) logger.Log($"{EndPoint} changed state from {State} to {state}");
            State = state;
        }
    }
}
