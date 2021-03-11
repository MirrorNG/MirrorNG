using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace Mirage.SocketLayer
{
    public class Time
    {
        public float Now => UnityEngine.Time.time;
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
        readonly Time time;

        readonly Dictionary<EndPoint, Connection> connections;

        readonly byte[] commandBuffer = new byte[3];

        public event Action<Connection> OnConnected;

        public Peer(ISocket socket, Config config)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
            this.config = config;
            time = new Time();
        }

        public void SendNotify() => throw new NotImplementedException();
        public void SendUnreliable() => throw new NotImplementedException();

        private void Send(Connection connection, Packet packet) => Send(connection, packet.data, packet.length);
        private void Send(Connection connection, byte[] data, int? length = null)
        {
            socket.Send(connection.EndPoint, data, length);
            connection.SetSendTime();
        }
        private void SendUnconnected(EndPoint endPoint, Packet packet) => SendUnconnected(endPoint, packet.data, packet.length);
        internal void SendUnconnected(EndPoint endPoint, byte[] data, int? length = null)
        {
            socket.Send(endPoint, data, length);
        }

        internal void SendCommandUnconnected(EndPoint endPoint, Commands command, byte? extra = null)
        {
            Packet packet = CreateCommandPacket(command, extra);
            SendUnconnected(endPoint, packet);
        }

        internal void SendCommand(Connection connection, Commands command, byte? extra = null)
        {
            Packet packet = CreateCommandPacket(command, extra);
            Send(connection, packet);
        }
        private Packet CreateCommandPacket(Commands command, byte? extra = null)
        {
            commandBuffer[0] = (byte)PacketType.Command;
            commandBuffer[1] = (byte)command;

            if (extra.HasValue)
            {
                commandBuffer[2] = extra.Value;
                return new Packet(commandBuffer, 3);
            }
            else
            {
                return new Packet(commandBuffer, 2);
            }
        }

        internal void SendKeepAlive(Connection connection)
        {
            commandBuffer[0] = (byte)PacketType.KeepAlive;
            Send(connection, commandBuffer, 1);
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

                var packet = new Packet(buffer, length);

                if (!packet.IsValidSize())
                {
                    // handle message that are too small
                    throw new NotImplementedException();
                }


                if (connections.TryGetValue(endPoint, out Connection connection))
                {
                    HandleMessage(connection, packet);
                }
                else
                {
                    HandleNewConnection(endPoint, packet);
                }
            }
        }

        private byte[] getBuffer()
        {
            throw new NotImplementedException();
        }


        private void HandleMessage(Connection connection, Packet packet)
        {
            switch (packet.type)
            {
                case PacketType.Command:
                    HandleCommand(connection, packet);
                    break;
                case PacketType.Unreliable:
                case PacketType.Notify:
                    // todo are these handled differently?
                    IMessageReceiver receiver = getReceiver(connection);
                    receiver.TransportReceive(packet.ToSegment());
                    break;
                case PacketType.KeepAlive:
                    // do nothing
                    break;
                default:
                    // handle message invalid packet type
                    throw new NotImplementedException();
            }

            connection.SetReceiveTime();
        }

        private void HandleCommand(Connection connection, Packet packet)
        {
            throw new NotImplementedException();
        }

        private void HandleNewConnection(EndPoint endPoint, Packet packet)
        {
            // if invalid, then reject without reason
            if (Validate(endPoint, packet)) { return; }

            if (AtMaxConnections())
            {
                RejectConnectionWithReason(endPoint, RejectReason.ServerFull);
            }
            else
            {
                AcceptNewConnection(endPoint);
            }
        }

        private bool Validate(EndPoint endPoint, Packet packet)
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

            var connection = new Connection(this, endPoint, config, time);
            connection.LastRecvPacketTime = time.Now;
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
                kvp.Value.Update();
            }
        }
    }
    internal struct Packet
    {
        const int MinSize = 1;

        public byte[] data;
        public int length;

        public Packet(byte[] data, int length)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            this.length = length;
        }

        public bool IsValidSize()
        {
            return length >= MinSize;
        }

        public PacketType type => (PacketType)data[0];

        public ArraySegment<byte> ToSegment()
        {
            return new ArraySegment<byte>(data, 0, length);
        }
    }

    internal enum PacketType
    {
        /// <summary>
        /// see <see cref="Commands"/>
        /// </summary>
        Command = 1,

        Unreliable = 2,
        Notify = 3,

        /// <summary>
        /// Used to keep connection alive.
        /// <para>Similar to ping/pong</para>
        /// </summary>
        KeepAlive = 4,
    }

    /// <summary>
    /// Small message used to control a connection
    /// <para>
    ///     <see cref="PacketType"/> and Commands uses their own byte/enum to split up the flow and add struture to the code.
    /// </para>
    /// </summary>
    internal enum Commands
    {
        /// <summary>
        /// Sent from client to request to connect to server
        /// </summary>
        ConnectRequest = 1,

        /// <summary>
        /// Sent when Server accepts client
        /// </summary>
        ConnectionAccepted = 2,

        /// <summary>
        /// Sent when server rejects client
        /// </summary>
        ConnectionRejected = 3,

        /// <summary>
        /// Sent from client or server to close connection
        /// </summary>
        Disconnect = 4,
    }

    /// <summary>
    /// Reson for reject sent from server
    /// </summary>
    internal enum RejectReason
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
