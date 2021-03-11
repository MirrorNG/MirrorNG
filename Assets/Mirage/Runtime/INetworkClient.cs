using UnityEngine.Events;

namespace Mirage
{
    public interface INetworkClient : IMessageSender
    {

        /// <summary>
        /// Event fires once the Client has connected its Server.
        /// </summary>
        NetworkConnectionEvent Connected { get; }

        /// <summary>
        /// Event fires after the Client connection has sucessfully been authenticated with its Server.
        /// </summary>
        NetworkConnectionEvent Authenticated { get; }

        /// <summary>
        /// Event fires after the Client has disconnected from its Server and Cleanup has been called.
        /// </summary>
        UnityEvent Disconnected { get; }

        /// <summary>
        /// The NetworkConnection object this client is using.
        /// </summary>
        INetworkPlayer Connection { get; }

        /// <summary>
        /// active is true while a client is connecting/connected
        /// (= while the network is active)
        /// </summary>
        bool Active { get; }

        /// <summary>
        /// NetworkClient can connect to local server in host mode too
        /// </summary>
        bool IsLocalClient { get; }

        void Disconnect();
    }
}
