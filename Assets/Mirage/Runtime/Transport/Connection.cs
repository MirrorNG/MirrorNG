using System;
using System.Net;
using UnityEngine;

namespace Mirage.SocketLayer
{
    public enum ConnectionState
    {
        /// <summary>
        /// Inital state
        /// </summary>
        Created = 1,
        /// <summary>
        /// Client is connecting to server
        /// </summary>
        Connecting = 2,
        /// <summary>
        /// Server as accepted connection
        /// </summary>
        Connected = 3,

        Disconnected = 9,
        Destroyed = 10,
    }

    public sealed class Connection
    {
        static readonly ILogger logger = LogFactory.GetLogger<Connection>();

        public ConnectionState State { get; private set; }

        private readonly Peer peer;
        public readonly EndPoint EndPoint;
        private readonly Config config;
        private readonly Time time;
        private ConnectingTracker connectingTracker;

        public Connection(Peer peer, EndPoint endPoint, Config config, Time time)
        {
            this.peer = peer;
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            this.config = config;
            this.time = time;
            State = ConnectionState.Created;
            connectingTracker = new ConnectingTracker(config, time);
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



        public void Update()
        {
            switch (State)
            {
                case ConnectionState.Connecting:
                    UpdateConnecting();
                    break;

                case ConnectionState.Connected:
                    UpdateConnected();
                    break;

                case ConnectionState.Disconnected:
                    UpdateDisconnected();
                    break;
            }
        }
        public void SetReceiveTime()
        {
            timeoutTracker.SetReceiveTime();
        }
        public void SetSendTime()
        {
            keepAliveTracker.SetSendTime();
        }


        /// <summary>
        /// client connecting attempts
        /// </summary>
        void UpdateConnecting()
        {
            if (connectingTracker.TimeAttempt())
            {
                if (connectingTracker.MaxAttempts())
                {
                    // client failed to connect
                }

                connectingTracker.OnAttempt();
                peer.SendCommand(this, Commands.ConnectRequest);
            }
        }
        void UpdateDisconnected()
        {
            // todo why not remove disconnected right away
            //if ((connection.DisconnectTime + _config.DisconnectIdleTime) < _clock.ElapsedInSeconds)
            //{
            //    RemoveConnection(connection);
            //}
        }

        /// <summary>
        /// Used
        /// </summary>
        void UpdateConnected()
        {
            if (timeoutTracker.TimeToDisconnect())
            {
                // disconnect here
                throw new NotImplementedException();
                return;
            }

            if (keepAliveTracker.TimeToSend())
            {
                peer.SendKeepAlive(this);
            }
        }


        TimeoutTracker timeoutTracker;
        KeepAliveTracker keepAliveTracker;
        class ConnectingTracker
        {
            private readonly Config config;
            private readonly Time time;
            float lastAttempt;
            int AttemptCount;

            public ConnectingTracker(Config config, Time time)
            {
                this.config = config;
                this.time = time;
            }

            public bool TimeAttempt()
            {
                return lastAttempt + config.ConnectAttemptInterval < time.Now;
            }

            internal bool MaxAttempts()
            {
                return AttemptCount >= config.MaxConnectAttempts;
            }

            internal void OnAttempt()
            {
                AttemptCount++;
                lastAttempt = time.Now;
            }
        }

        class TimeoutTracker
        {
            float lastRecvTime;
            readonly Config config;
            readonly Time time;

            public TimeoutTracker(Config config, Time time)
            {
                this.config = config;
                this.time = time ?? throw new ArgumentNullException(nameof(time));
            }

            public bool TimeToDisconnect()
            {
                return lastRecvTime + config.DisconnectTimeout < time.Now;
            }

            public void SetReceiveTime()
            {
                lastRecvTime = time.Now;
            }
        }
        class KeepAliveTracker
        {
            float lastSendTime;
            readonly Config config;
            readonly Time time;

            public KeepAliveTracker(Config config, Time time)
            {
                this.config = config;
                this.time = time ?? throw new ArgumentNullException(nameof(time));
            }


            public bool TimeToSend()
            {
                return lastSendTime + config.KeepAliveInterval < time.Now;
            }

            internal void SetSendTime()
            {
                lastSendTime = time.Now;
            }
        }
        class DisconnectedTracker { }
    }
}
