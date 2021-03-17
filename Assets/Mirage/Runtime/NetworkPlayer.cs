using System;
using System.Collections.Generic;
using System.Net;
using Mirage.Logging;
using UnityEngine;
using UnityEngine.Assertions;

namespace Mirage
{

    [NetworkMessage]
    public struct NotifyAck
    {
    }

    /// <summary>
    /// A High level network connection. This is used for connections from client-to-server and for connection from server-to-client.
    /// </summary>
    /// <remarks>
    /// <para>A NetworkConnection corresponds to a specific connection for a host in the transport layer. It has a connectionId that is assigned by the transport layer and passed to the Initialize function.</para>
    /// <para>A NetworkClient has one NetworkConnection. A NetworkServerSimple manages multiple NetworkConnections. The NetworkServer has multiple "remote" connections and a "local" connection for the local client.</para>
    /// <para>The NetworkConnection class provides message sending and handling facilities. For sending data over a network, there are methods to send message objects, byte arrays, and NetworkWriter objects. To handle data arriving from the network, handler functions can be registered for message Ids, byte arrays can be processed by HandleBytes(), and NetworkReader object can be processed by HandleReader().</para>
    /// <para>NetworkConnection objects also act as observers for networked objects. When a connection is an observer of a networked object with a NetworkIdentity, then the object will be visible to corresponding client for the connection, and incremental state changes will be sent to the client.</para>
    /// <para>There are many virtual functions on NetworkConnection that allow its behaviour to be customized. NetworkClient and NetworkServer can both be made to instantiate custom classes derived from NetworkConnection by setting their networkConnectionClass member variable.</para>
    /// </remarks>
    public class NetworkPlayer : INetworkPlayer
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkPlayer));


        // internal so it can be tested
        private readonly HashSet<NetworkIdentity> visList = new HashSet<NetworkIdentity>();


        /// <summary>
        /// General purpose object to hold authentication data, character selection, tokens, etc.
        /// associated with the connection for reference after Authentication completes.
        /// </summary>
        public object AuthenticationData { get; set; }

        /// <summary>
        /// Flag that tells if the connection has been marked as "ready" by a client calling ClientScene.Ready().
        /// <para>This property is read-only. It is set by the system on the client when ClientScene.Ready() is called, and set by the system on the server when a ready message is received from a client.</para>
        /// <para>A client that is ready is sent spawned objects by the server and updates to the state of spawned objects. A client that is not ready is not sent spawned objects.</para>
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// The IP address / URL / FQDN associated with the connection.
        /// Can be useful for a game master to do IP Bans etc.
        /// </summary>
        public virtual EndPoint Address => Connection.GetEndPointAddress();

        /// <summary>
        /// Transport level connection
        /// </summary>
        /// <remarks>
        /// <para>On a server, this Id is unique for every connection on the server. On a client this Id is local to the client, it is not the same as the Id on the server for this connection.</para>
        /// <para>Transport layers connections begin at one. So on a client with a single connection to a server, the connectionId of that connection will be one. In NetworkServer, the connectionId of the local connection is zero.</para>
        /// <para>Clients do not know their connectionId on the server, and do not know the connectionId of other clients on the server.</para>
        /// </remarks>
        public IConnection Connection { get; }
        public IMessageHandler MessageHandler { get; }

        /// <summary>
        /// The NetworkIdentity for this connection.
        /// </summary>
        public NetworkIdentity Identity { get; set; }

        /// <summary>
        /// A list of the NetworkIdentity objects owned by this connection. This list is read-only.
        /// <para>This includes the player object for the connection - if it has localPlayerAutority set, and any objects spawned with local authority or set with AssignLocalAuthority.</para>
        /// <para>This list can be used to validate messages from clients, to ensure that clients are only trying to control objects that they own.</para>
        /// </summary>
        // IMPORTANT: this needs to be <NetworkIdentity>, not <uint netId>. fixes a bug where DestroyOwnedObjects wouldn't find
        //            the netId anymore: https://github.com/vis2k/Mirror/issues/1380 . Works fine with NetworkIdentity pointers though.
        private readonly HashSet<NetworkIdentity> clientOwnedObjects = new HashSet<NetworkIdentity>();

        /// <summary>
        /// Creates a new NetworkConnection with the specified address and connectionId
        /// </summary>
        /// <param name="networkConnectionId"></param>
        public NetworkPlayer(IConnection connection, IMessageHandler messageHandler)
        {
            Assert.IsNotNull(connection);
            Connection = connection;
            MessageHandler = messageHandler;
        }

        public override string ToString()
        {
            return $"connection({Address})";
        }

        public void AddToVisList(NetworkIdentity identity)
        {
            visList.Add(identity);
        }

        public void RemoveFromVisList(NetworkIdentity identity)
        {
            visList.Remove(identity);
        }

        public void RemoveObservers()
        {
            foreach (NetworkIdentity identity in visList)
            {
                identity.RemoveObserverInternal(this);
            }
            visList.Clear();
        }

        public void AddOwnedObject(NetworkIdentity networkIdentity)
        {
            clientOwnedObjects.Add(networkIdentity);
        }

        public void RemoveOwnedObject(NetworkIdentity networkIdentity)
        {
            clientOwnedObjects.Remove(networkIdentity);
        }

        public void DestroyOwnedObjects()
        {
            // create a copy because the list might be modified when destroying
            var tmp = new HashSet<NetworkIdentity>(clientOwnedObjects);
            foreach (NetworkIdentity netIdentity in tmp)
            {
                //dont destroy self yet.
                if (netIdentity != null && netIdentity != Identity && Identity.ServerObjectManager != null)
                {
                    Identity.ServerObjectManager.Destroy(netIdentity.gameObject);
                }
            }

            if (Identity != null && Identity.Server != null)
                // Destroy the connections own identity.
                Identity.ServerObjectManager.Destroy(Identity.gameObject);

            // clear the hashset because we destroyed them all
            clientOwnedObjects.Clear();
        }

        public void Send<T>(T message, int channelId = 0)
        {
            MessageHandler.Send(this, message, channelId);
        }

        public void Send(ArraySegment<byte> segment, int channelId = 0)
        {
            MessageHandler.Send(this, segment, channelId);
        }
    }
}
