using System.Net;
using System.Net.Sockets;

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

        public void Recieve(byte[] buffer, ref EndPoint endPoint, out int bytesReceived)
        {
            // todo do we need to set if null
            endPoint = endPoint ?? new IPEndPoint(IPAddress.Any, 0);
            bytesReceived = socket.ReceiveFrom(buffer, ref endPoint);
        }

        public void Send(EndPoint endPoint, byte[] data)
        {
            // todo check disconnected
            socket.SendTo(data, (IPEndPoint)endPoint);
        }
    }
}
