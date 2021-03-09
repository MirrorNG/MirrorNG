using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Mirage
{
    public class UDPSocket : ISocket
    {
        readonly Socket socket;

        public UDPSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        /// <summary>
        /// Is message avaliable
        /// </summary>
        /// <returns>true if data to read</returns>
        public bool Poll()
        {
            return socket.Poll(0, SelectMode.SelectRead);
        }

        public void RawRecieve(byte[] buffer, ref EndPoint endPoint, out int bytesReceived)
        {
            // todo do we need to set if null
            endPoint = endPoint ?? new IPEndPoint(IPAddress.Any, 0);
            bytesReceived = socket.ReceiveFrom(buffer, ref endPoint);
        }

        public void RawSend(EndPoint endPoint, byte[] data)
        {
            // todo check disconnected
            socket.SendTo(data, (IPEndPoint)endPoint);
        }
    }
    /// <summary>
    /// Link between Mirage and the outside world...
    /// </summary>
    public interface ISocket
    {
        /// <summary>
        /// Is message avaliable
        /// </summary>
        /// <returns>true if data to read</returns>
        bool Poll();

        /// <summary>
        /// Gets next Message
        /// <para>Should be called after Poll</para>
        /// </summary>
        /// <param name="data">recieved data</param>
        void RawRecieve(byte[] data, ref EndPoint endPoint, out int bytesReceived);

        /// <summary>
        /// Sends to 
        /// </summary>
        /// <param name="data"></param>
        void RawSend(EndPoint endPoint, byte[] data);
    }

    public sealed class Connection
    {
        public readonly EndPoint EndPoint;

        public Connection(EndPoint endPoint)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }
    }

    /// <summary>
    /// Controls flow of data in/out of mirage, Uses <see cref="ISocket"/>
    /// </summary>
    public sealed class Peer
    {
        // todo SendUnreliable
        // tood SendNotify

        readonly ISocket socket;

        readonly Dictionary<EndPoint, Connection> connections;


        public Peer(ISocket socket)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public void Send(Connection connection, byte[] data)
        {
            socket.RawSend(connection.EndPoint, data);
        }
    }

    /// <summary>
    /// Creates <see cref="ISocket"/>
    /// </summary>
    /// <remarks>
    /// The only job of Transport is to create a <see cref="ISocket"/> that will be used by mirage to send/recieve data.
    /// <para>This is a MonoBehaviour so can be attached in the inspector</para>
    /// </remarks>
    public abstract class TransportV2 : MonoBehaviour
    {
        public abstract ISocket CreateSocket();
    }

    [System.Obsolete("Use TransportV2, Peer, and ISocket instead", true)]
    public abstract class Transport : MonoBehaviour
    {
        public class ConnectEvent : UnityEvent<IConnection> { }

        public abstract IEnumerable<string> Scheme { get; }

        /// <summary>
        /// Event that gets fired when a client is accepted by the transport
        /// </summary>
        public ConnectEvent Connected = new ConnectEvent();

        /// <summary>
        /// Raised when the transport starts
        /// </summary>
        public UnityEvent Started = new UnityEvent();

        /// <summary>
        /// Open up the port and listen for connections
        /// Use in servers.
        /// Note the task ends when we stop listening
        /// </summary>
        /// <exception>If we cannot start the transport</exception>
        /// <returns></returns>
        public abstract UniTask ListenAsync();

        /// <summary>
        /// Stop listening to the port
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// Determines if this transport is supported in the current platform
        /// </summary>
        /// <returns>true if the transport works in this platform</returns>
        public abstract bool Supported { get; }

        /// <summary>
        /// Connect to a server located at a provided uri
        /// </summary>
        /// <param name="uri">address of the server to connect to</param>
        /// <returns>The connection to the server</returns>
        /// <exception>If connection cannot be established</exception>
        public abstract UniTask<IConnection> ConnectAsync(Uri uri);

        /// <summary>
        /// Retrieves the address of this server.
        /// Useful for network discovery
        /// </summary>
        /// <returns>the url at which this server can be reached</returns>
        public abstract IEnumerable<Uri> ServerUri();

        /// <summary>
        /// Gets the total amount of received data
        /// </summary>
        public virtual long ReceivedBytes => 0;

        /// <summary>
        /// Gets the total amount of sent data
        /// </summary>
        public virtual long SentBytes => 0;
    }
}
