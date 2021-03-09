using System.Net;

namespace Mirage.SocketLayer
{
    /// <summary>
    /// Link between Mirage and the outside world...
    /// </summary>
    public interface ISocket
    {
        /// <summary>
        /// Starts listens for data on an endpoint
        /// <para>Used by Server to accpet data from clients</para>
        /// </summary>
        /// <param name="endPoint"></param>
        void Bind(EndPoint endPoint);

        /// <summary>
        /// Connects to an endpoint
        /// <para>Used by clients to connect to server</para>
        /// </summary>
        /// <param name="endPoint"></param>
        void Connect(EndPoint endPoint);

        void Close();

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
        void Recieve(byte[] data, ref EndPoint endPoint, out int bytesReceived);

        /// <summary>
        /// Sends to 
        /// </summary>
        /// <param name="data"></param>
        void Send(EndPoint endPoint, byte[] data, int? length);
    }
}
