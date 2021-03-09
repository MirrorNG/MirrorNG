using System.Net;

namespace Mirage
{
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
        void Recieve(byte[] data, ref EndPoint endPoint, out int bytesReceived);

        /// <summary>
        /// Sends to 
        /// </summary>
        /// <param name="data"></param>
        void Send(EndPoint endPoint, byte[] data);

        void Close();

        // todo do we ever need to bind to endpoint? (rather than null)
        void Bind(EndPoint endPoint);
    }
}
