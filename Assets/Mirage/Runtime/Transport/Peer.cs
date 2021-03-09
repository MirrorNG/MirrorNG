using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace Mirage
{
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
            socket.Send(connection.EndPoint, data);
        }

        public void ReceiveLoop()
        {
            byte[] buffer = getBuffer();
            while (socket.Poll())
            {
                //todo do we need to pass in endpoint?
                EndPoint endPoint = null;
                socket.Recieve(buffer, ref endPoint, out int length);

                IMessageReceiver receiver = getReceiver();
                receiver.TransportReceive(new ArraySegment<byte>(buffer, 0, length));
            }
        }

        private IMessageReceiver getReceiver()
        {
            throw new NotImplementedException();
        }

        private byte[] getBuffer()
        {
            throw new NotImplementedException();
        }
    }

    public sealed class Connection
    {
        public readonly EndPoint EndPoint;

        public Connection(EndPoint endPoint)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }
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
