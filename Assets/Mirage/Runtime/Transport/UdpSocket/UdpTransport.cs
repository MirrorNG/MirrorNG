using System.Net;
using System.Net.Sockets;
using Mirage.SocketLayer;
using UnityEngine;

namespace Mirage
{
    public sealed class UdpTransport : TransportV2
    {
        [SerializeField] string address;
        [SerializeField] int port;

        public override ISocket CreateClientSocket()
        {
            return new UDPSocket();
        }

        public override ISocket CreateServerSocket()
        {
            return new UDPSocket();
        }

        public override EndPoint GetBindEndPoint()
        {
            return new IPEndPoint(IPAddress.Any, port);
        }
        public override EndPoint GetConnectEndPoint()
        {
            return new IPEndPoint(IPAddress.Parse(address), port);
        }

        public override bool ClientSupported => platformNotWebgl;
        public override bool ServerSupported => platformNotWebgl;
        private static bool platformNotWebgl => Application.platform != RuntimePlatform.WebGLPlayer;
    }

    public class UDPSocket : ISocket
    {
        readonly Socket socket;

        public UDPSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public void Bind(EndPoint endPoint)
        {
            socket.Bind(endPoint);
        }

        public void Connect(EndPoint endPoint)
        {
            socket.Connect(endPoint);
        }

        public void Close()
        {
            socket.Close();
            socket.Dispose();
        }

        /// <summary>
        /// Is message avaliable
        /// </summary>
        /// <returns>true if data to read</returns>
        public bool Poll()
        {
            return socket.Poll(0, SelectMode.SelectRead);
        }

        public void Recieve(byte[] buffer, ref EndPoint endPoint, out int bytesReceived)
        {
            // todo do we need to set if null
            endPoint = endPoint ?? new IPEndPoint(IPAddress.Any, 0);
            bytesReceived = socket.ReceiveFrom(buffer, ref endPoint);
        }

        public void Send(EndPoint endPoint, byte[] data, int? length = null)
        {
            int size = length ?? data.Length;
            // todo check disconnected
            // todo what SocketFlags??
            socket.SendTo(data, size, SocketFlags.None, (IPEndPoint)endPoint);
        }
    }
}
