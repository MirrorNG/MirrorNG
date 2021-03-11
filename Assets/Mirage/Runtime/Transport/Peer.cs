using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace Mirage.SocketLayer
{
    public struct Config
    {
        public int MaxConnections;
    }
    /// <summary>
    /// Controls flow of data in/out of mirage, Uses <see cref="ISocket"/>
    /// </summary>
    public sealed class Peer
    {
        static readonly ILogger logger = LogFactory.GetLogger<Peer>();

        // todo SendUnreliable
        // tood SendNotify

        readonly ISocket socket;
        readonly Config config;

        readonly Dictionary<EndPoint, Connection> connections;

        readonly byte[] commandBuffer = new byte[3];

        public event Action<Connection> OnConnected;

        public Peer(ISocket socket, Config config)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            this.config = config;
        }

        public void SendNotify() => throw new NotImplementedException();
        public void SendUnreliable() => throw new NotImplementedException();

        private void Send(Connection connection, byte[] data, int? length = null)
        {
            socket.Send(connection.EndPoint, data, length);
        }
        private void SendUnconnected(EndPoint endPoint, byte[] data, int? length = null)
        {
            socket.Send(endPoint, data, length);
        }

        private void SendCommandUnconnected(EndPoint endPoint, Commands command, byte? extra = null)
        {
            commandBuffer[0] = (byte)PacketType.Command;
            commandBuffer[1] = (byte)command;

            if (extra.HasValue)
            {
                commandBuffer[2] = extra.Value;
                SendUnconnected(endPoint, commandBuffer, 3);
            }
            else
            {
                SendUnconnected(endPoint, commandBuffer, 2);
            }
        }

        private void SendCommand(Connection connection, Commands command, byte? extra = null)
        {
            commandBuffer[0] = (byte)PacketType.Command;
            commandBuffer[1] = (byte)command;

            if (extra.HasValue)
            {
                commandBuffer[2] = extra.Value;
                Send(connection, commandBuffer, 3);
            }
            else
            {
                Send(connection, commandBuffer, 2);
            }
        }

        public void Tick()
        {
            ReceiveLoop();
            UpdateConnections();
        }

        private void ReceiveLoop()
        {
            byte[] buffer = getBuffer();
            while (socket.Poll())
            {
                //todo do we need to pass in endpoint?
                EndPoint endPoint = null;
                socket.Recieve(buffer, ref endPoint, out int length);

                var segment = new ArraySegment<byte>(buffer, 0, length);
                if (connections.TryGetValue(endPoint, out Connection connection))
                {
                    HandleMessage(connection, segment);
                }
                else
                {
                    HandleNewConnection(endPoint, segment);
                }
            }
        }

        private byte[] getBuffer()
        {
            throw new NotImplementedException();
        }

        private void HandleMessage(Connection connection, ArraySegment<byte> segment)
        {
            IMessageReceiver receiver = getReceiver(connection);
            receiver.TransportReceive(segment);
        }
        private void HandleNewConnection(EndPoint endPoint, ArraySegment<byte> segment)
        {
            // if invalid, then reject without reason
            if (Validate(endPoint, segment)) { return; }

            if (AtMaxConnections())
            {
                RejectConnectionWithReason(endPoint, RejectReason.ServerFull);
            }
            else
            {
                AcceptNewConnection(endPoint);
            }
        }

        private bool Validate(EndPoint endPoint, ArraySegment<byte> segment)
        {
            // todo do security stuff here:
            // - connect request
            // - simple key/phrase send from client with first message
            // - hashcash??
            return true;
        }

        private bool AtMaxConnections()
        {
            return connections.Count >= config.MaxConnections;
        }
        private void AcceptNewConnection(EndPoint endPoint)
        {
            if (logger.LogEnabled()) logger.Log($"Accepting new connection from:{endPoint}");

            var connection = new Connection(endPoint, config);
            connection.LastRecvPacketTime = Time.time;
            connections.Add(endPoint, connection);

            switch (connection.State)
            {
                case ConnectionState.Created:
                    connection.ChangeState(ConnectionState.Connected);
                    OnConnected?.Invoke(connection);
                    SendCommand(connection, Commands.ConnectionAccepted);
                    break;

                case ConnectionState.Connected:
                    // send command again, unreliable so first message could have been missed
                    SendCommand(connection, Commands.ConnectionAccepted);
                    break;

                case ConnectionState.Connecting:
                    // todo use better Exception type
                    throw new Exception($"Server connections should not be in {nameof(ConnectionState.Connecting)} state");
            }
        }

        private void RejectConnectionWithReason(EndPoint endPoint, RejectReason reason)
        {
            SendCommandUnconnected(endPoint, Commands.ConnectionAccepted, (byte)reason);
        }

        private IMessageReceiver getReceiver(Connection connection)
        {
            throw new NotImplementedException();
        }


        void UpdateConnections()
        {
            foreach (KeyValuePair<EndPoint, Connection> kvp in connections)
            {
                UpdateConnection(kvp.Value);
            }
        }
        void UpdateConnection(Connection connection)
        {
            switch (connection.State)
            {
                case ConnectionState.Connecting:
                    UpdateConnecting(connection);
                    break;

                case ConnectionState.Connected:
                    UpdateConnected(connection);
                    break;

                case ConnectionState.Disconnected:
                    UpdateDisconnected(connection);
                    break;
            }
        }

        void UpdateConnecting(Connection connection)
        {
            if ((connection.ConnectionAttemptTime + _config.ConnectAttemptInterval) < _clock.ElapsedInSeconds)
            {
                if (connection.ConnectionAttempts == _config.MaxConnectAttempts)
                {
                    Assert.AlwaysFail("connection failed handle this with a callback");
                    return;
                }

                connection.ConnectionAttempts += 1;
                connection.ConnectionAttemptTime = _clock.ElapsedInSeconds;

                SendCommand(connection, Commands.ConnectRequest);
            }
        }
        void UpdateDisconnected(Connection connection)
        {
            if ((connection.DisconnectTime + _config.DisconnectIdleTime) < _clock.ElapsedInSeconds)
            {
                RemoveConnection(connection);
            }
        }

        void UpdateConnected(Connection connection)
        {
            if ((connection.LastRecvPacketTime + _config.ConnectionTimeout) < _clock.ElapsedInSeconds)
            {
                DisconnectConnection(connection, DisconnectedReason.Timeout);
            }

            if ((connection.LastSentPacketTime + _config.KeepAliveInterval) < _clock.ElapsedInSeconds)
            {
                Send(connection, new byte[1] { (byte)PacketTypes.KeepAlive });
            }
        }
    }


    public enum PacketType
    {
        Command = 1,
        Unreliable = 2,
        Notify = 3,
        KeepAlive = 4
    }

    public enum Commands
    {
        None = 0,
        ConnectionAccepted = 1,
        ConnectionRejected = 1,
    }
    enum RejectReason
    {
        None = 0,
        ServerFull = 1,
    }
    // todo how should we use this?
    public sealed class PeerDebug
    {
        public int ReceivedBytes { get; set; }
        public int SentBytes { get; set; }
    }


    /// <summary>
    /// Creates <see cref="ISocket"/>
    /// </summary>
    /// <remarks>
    /// The only job of Transport is to create a <see cref="ISocket"/> that will be used by mirage to send/recieve data.
    /// <para>This is a MonoBehaviour so can be attached in the inspector</para>
    /// </remarks>
    // todo rename this to Transport when finished
    public abstract class TransportV2 : MonoBehaviour
    {
        public abstract ISocket CreateClientSocket();
        public abstract ISocket CreateServerSocket();

        public abstract bool ClientSupported { get; }
        public abstract bool ServerSupported { get; }
    }
}
