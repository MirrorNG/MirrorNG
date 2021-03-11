using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Mirage.SocketLayer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Mirage
{
    public static class PeerUtil
    {
        public static (Peer, TransportV2) Create(GameObject holder)
        {
            TransportV2 socketCreator;
            socketCreator = holder.GetComponent<TransportV2>();

            PeerUpdater peerUpdater = holder.GetComponent<PeerUpdater>();

            if (socketCreator == null)
                throw new InvalidOperationException("Transport could not be found");

            if (peerUpdater == null)
                throw new InvalidOperationException("PeerUpdater could not be found");

            ISocket socket = socketCreator.CreateServerSocket();
            var peer = new Peer(socket, new Config
            {
                // todo expose these setting
                MaxConnections = 4,
                MaxConnectAttempts = 10,
                ConnectAttemptInterval = 2,
                DisconnectTimeout = 30,
                KeepAliveInterval = 10,
            });

            peerUpdater.peer = peer;
            return (peer, socketCreator);
        }
    }

    /// <summary>
    /// The NetworkServer.
    /// </summary>
    /// <remarks>
    /// <para>NetworkServer handles remote connections from remote clients, and also has a local connection for a local client.</para>
    /// </remarks>
    [AddComponentMenu("Network/NetworkServer")]
    [DisallowMultipleComponent]
    public class NetworkServer : MonoBehaviour, INetworkServer
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkServer));

        bool initialized;

        /// <summary>
        /// The maximum number of concurrent network connections to support.
        /// <para>This effects the memory usage of the network layer.</para>
        /// </summary>
        [Tooltip("Maximum number of concurrent connections.")]
        [Min(1)]
        public int MaxConnections = 4;

        /// <summary>
        /// <para>If you disable this, the server will not listen for incoming connections on the regular network port.</para>
        /// <para>This can be used if the game is running in host mode and does not want external players to be able to connect - making it like a single-player game. Also this can be useful when using AddExternalConnection().</para>
        /// </summary>
        public bool Listening = true;

        // transport to use to accept connections
        public Peer peer;
        public TransportV2 socketCreator;

        [Tooltip("Authentication component attached to this object")]
        public NetworkAuthenticator authenticator;

        [Header("Events")]
        /// <summary>
        /// This is invoked when a server is started - including when a host is started.
        /// </summary>
        [FormerlySerializedAs("Started")]
        [SerializeField] UnityEvent _started = new UnityEvent();
        public UnityEvent Started => _started;

        /// <summary>
        /// Event fires once a new Client has connect to the Server.
        /// </summary>
        [FormerlySerializedAs("Connected")]
        [SerializeField] NetworkConnectionEvent _connected = new NetworkConnectionEvent();
        public NetworkConnectionEvent Connected => _connected;

        /// <summary>
        /// Event fires once a new Client has passed Authentication to the Server.
        /// </summary>
        [FormerlySerializedAs("Authenticated")]
        [SerializeField] NetworkConnectionEvent _authenticated = new NetworkConnectionEvent();
        public NetworkConnectionEvent Authenticated => _authenticated;

        /// <summary>
        /// Event fires once a Client has Disconnected from the Server.
        /// </summary>
        [FormerlySerializedAs("Disconnected")]
        [SerializeField] NetworkConnectionEvent _disconnected = new NetworkConnectionEvent();
        public NetworkConnectionEvent Disconnected => _disconnected;

        [SerializeField] UnityEvent _stopped = new UnityEvent();
        public UnityEvent Stopped => _stopped;

        /// <summary>
        /// This is invoked when a host is started.
        /// <para>StartHost has multiple signatures, but they all cause this hook to be called.</para>
        /// </summary>
        [SerializeField] UnityEvent _onStartHost = new UnityEvent();
        public UnityEvent OnStartHost => _onStartHost;

        /// <summary>
        /// This is called when a host is stopped.
        /// </summary>
        [SerializeField] UnityEvent _onStopHost = new UnityEvent();
        public UnityEvent OnStopHost => _onStopHost;

        /// <summary>
        /// The connection to the host mode client (if any).
        /// </summary>
        // original HLAPI has .localConnections list with only m_LocalConnection in it
        // (for backwards compatibility because they removed the real localConnections list a while ago)
        // => removed it for easier code. use .localConnection now!
        public INetworkPlayer LocalConnection { get; private set; }

        /// <summary>
        /// The host client for this server 
        /// </summary>
        public NetworkClient LocalClient { get; private set; }

        /// <summary>
        /// True if there is a local client connected to this server (host mode)
        /// </summary>
        public bool LocalClientActive => LocalClient != null && LocalClient.Active;

        /// <summary>
        /// Number of active player objects across all connections on the server.
        /// <para>This is only valid on the host / server.</para>
        /// </summary>
        public int NumberOfPlayers => connections.Count(kv => kv.Identity != null);

        /// <summary>
        /// A list of local connections on the server.
        /// </summary>
        public readonly HashSet<INetworkPlayer> connections = new HashSet<INetworkPlayer>();

        /// <summary>
        /// <para>Checks if the server has been started.</para>
        /// <para>This will be true after NetworkServer.Listen() has been called.</para>
        /// </summary>
        public bool Active { get; private set; }

        readonly NetworkTime _time = new NetworkTime();
        /// <summary>
        /// Time kept in this server
        /// </summary>
        public NetworkTime Time
        {
            get { return _time; }
        }

        /// <summary>
        /// This shuts down the server and disconnects all clients.
        /// </summary>
        public void Disconnect()
        {
            if (LocalClient != null)
            {
                OnStopHost?.Invoke();
                LocalClient.Disconnect();
            }

            // make a copy,  during disconnect, it is possible that connections
            // are modified, so it throws
            // System.InvalidOperationException : Collection was modified; enumeration operation may not execute.
            var connectionscopy = new HashSet<INetworkPlayer>(connections);
            foreach (INetworkPlayer conn in connectionscopy)
            {
                conn.Disconnect();
            }
            peer?.Close();
        }

        void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            Application.quitting += Disconnect;
            if (logger.LogEnabled()) logger.Log($"NetworkServer Created, Mirage version: {Version.Current}");


            //Make sure connections are cleared in case any old connections references exist from previous sessions
            connections.Clear();

            (peer, socketCreator) = PeerUtil.Create(gameObject);

            if (authenticator != null)
            {
                authenticator.OnServerAuthenticated += OnAuthenticated;

                Connected.AddListener(authenticator.OnServerAuthenticateInternal);
            }
            else
            {
                // if no authenticator, consider every connection as authenticated
                Connected.AddListener(OnAuthenticated);
            }
        }

        /// <summary>
        /// Start the server, setting the maximum number of connections.
        /// </summary>
        /// <param name="maxConns">Maximum number of allowed connections</param>
        /// <returns></returns>
        public void Listen()
        {
            Initialize();

            try
            {
                // only start server if we want to listen
                if (Listening)
                {
                    Started.Invoke();
                    peer.OnConnected += TransportConnected;
                    peer.Bind(socketCreator.GetBindEndPoint());
                }
                else
                {
                    // if not listening then call started events right away
                    NotListeningStarted();
                }

                Active = true;
                // (useful for loading & spawning stuff from database etc.)
                Started?.Invoke();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }
            finally
            {
                Cleanup();
                // clear reference to peer, we can create new instance each time we start
                peer = null;
            }
        }

        private void NotListeningStarted()
        {
            logger.Log("Server started but not Listening");
        }

        private void TransportStarted()
        {
            logger.Log("Server started listening");
        }

        private void TransportConnected(Connection connection)
        {
            INetworkPlayer networkConnectionToClient = GetNewConnection(connection);
            ConnectionAcceptedAsync(networkConnectionToClient).Forget();
        }

        /// <summary>
        /// This starts a network "host" - a server and client in the same application.
        /// <para>The client returned from StartHost() is a special "local" client that communicates to the in-process server using a message queue instead of the real network. But in almost all other cases, it can be treated as a normal client.</para>
        /// </summary>
        public void StartHost(NetworkClient client)
        {
            if (!client)
                throw new InvalidOperationException("NetworkClient not assigned. Unable to StartHost()");

            // start listening to network connections
            Listen();

            Active = true;

            client.ConnectHost(this);

            // call OnStartHost AFTER SetupServer. this way we can use
            // NetworkServer.Spawn etc. in there too. just like OnStartServer
            // is called after the server is actually properly started.
            OnStartHost?.Invoke();

            logger.Log("NetworkServer StartHost");
        }

        /// <summary>
        /// This stops both the client and the server that the manager is using.
        /// </summary>
        public void StopHost()
        {
            Disconnect();
        }

        /// <summary>
        /// cleanup resources so that we can start again
        /// </summary>
        private void Cleanup()
        {

            if (authenticator != null)
            {
                authenticator.OnServerAuthenticated -= OnAuthenticated;
                Connected.RemoveListener(authenticator.OnServerAuthenticateInternal);
            }
            else
            {
                // if no authenticator, consider every connection as authenticated
                Connected.RemoveListener(OnAuthenticated);
            }

            Stopped?.Invoke();
            initialized = false;
            Active = false;
        }

        /// <summary>
        /// Creates a new INetworkConnection based on the provided IConnection.
        /// </summary>
        public virtual INetworkPlayer GetNewConnection(Connection connection)
        {
            return new NetworkPlayer(connection);
        }

        /// <summary>
        /// <para>This accepts a network connection and adds it to the server.</para>
        /// <para>This connection will use the callbacks registered with the server.</para>
        /// </summary>
        /// <param name="conn">Network connection to add.</param>
        public void AddConnection(INetworkPlayer conn)
        {
            if (!connections.Contains(conn))
            {
                // connection cannot be null here or conn.connectionId
                // would throw NRE
                connections.Add(conn);
            }
        }

        /// <summary>
        /// This removes an external connection added with AddExternalConnection().
        /// </summary>
        /// <param name="connectionId">The id of the connection to remove.</param>
        public void RemoveConnection(INetworkPlayer conn)
        {
            connections.Remove(conn);
        }

        /// <summary>
        /// called by LocalClient to add itself. dont call directly.
        /// </summary>
        /// <param name="client">The local client</param>
        /// <param name="tconn">The connection to the client</param>
        internal void SetLocalConnection(NetworkClient client, IConnection tconn)
        {
            if (LocalConnection != null)
            {
                throw new InvalidOperationException("Local Connection already exists");
            }

            // todo work out how to handle local connection
            throw new NotImplementedException();

            //INetworkConnection conn = GetNewConnection(tconn);
            //LocalConnection = conn;
            //LocalClient = client;

            //ConnectionAcceptedAsync(conn).Forget();
        }

        /// <summary>
        /// Send a message to all connected clients.
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="msg">Message</param>
        /// <param name="channelId">Transport channel to use</param>
        public void SendToAll<T>(T msg, bool reliable)
        {
            if (logger.LogEnabled()) logger.Log("Server.SendToAll id:" + typeof(T));
            SendToMany(connections.Select(x => x.Connection), msg, reliable);
        }

        async UniTaskVoid ConnectionAcceptedAsync(INetworkPlayer conn)
        {
            if (logger.LogEnabled()) logger.Log("Server accepted client:" + conn);

            // are more connections allowed? if not, kick
            // (it's easier to handle this in Mirage, so Transports can have
            //  less code and third party transport might not do that anyway)
            // (this way we could also send a custom 'tooFull' message later,
            //  Transport can't do that)
            if (connections.Count >= MaxConnections)
            {
                conn.Disconnect();
                if (logger.WarnEnabled()) logger.LogWarning("Server full, kicked client:" + conn);
                return;
            }

            // add connection
            AddConnection(conn);

            // let everyone know we just accepted a connection
            Connected?.Invoke(conn);

            // now process messages until the connection closes
            try
            {
                await conn.ProcessMessagesAsync();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }
            finally
            {
                OnDisconnected(conn);
            }
        }

        //called once a client disconnects from the server
        void OnDisconnected(INetworkPlayer connection)
        {
            if (logger.LogEnabled()) logger.Log("Server disconnect client:" + connection);

            RemoveConnection(connection);

            Disconnected?.Invoke(connection);

            connection.DestroyOwnedObjects();
            connection.Identity = null;

            if (connection == LocalConnection)
                LocalConnection = null;
        }

        internal void OnAuthenticated(INetworkPlayer conn)
        {
            if (logger.LogEnabled()) logger.Log("Server authenticate client:" + conn);

            Authenticated?.Invoke(conn);
        }

        public static void SendToMany<T>(IEnumerable<Connection> connections, T msg, bool reliable = true)
        {
            // todo remove channel
            using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
            {
                // pack message into byte[] once
                MessagePacker.Pack(msg, writer);
                var segment = writer.ToArraySegment();
                int count = 0;

                foreach (Connection conn in connections)
                {
                    if (reliable)
                        conn.SendReliable(segment);
                    else
                        conn.SendUnreiable(segment);
                    // send to all connections, but don't wait for them
                    conn.SendUnreiable(segment);
                    count++;
                }

                NetworkDiagnostics.OnSend(msg, reliable, segment.Count, count);
            }
        }
    }
}
