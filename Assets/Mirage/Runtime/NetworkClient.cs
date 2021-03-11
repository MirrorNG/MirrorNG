using System;
using System.Net;
using Mirage.SocketLayer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Mirage
{

    /// <summary>
    /// Event fires from a <see cref="NetworkClient">NetworkClient</see> or <see cref="NetworkServer">NetworkServer</see> during a new connection, a new authentication, or a disconnection.
    /// <para>INetworkConnection - connection creating the event</para>
    /// </summary>
    [Serializable] public class NetworkConnectionEvent : UnityEvent<INetworkPlayer> { }

    public enum ConnectState
    {
        Disconnected,
        Connecting,
        Connected,
    }

    /// <summary>
    /// This is a network client class used by the networking system. It contains a NetworkConnection that is used to connect to a network server.
    /// <para>The <see cref="NetworkClient">NetworkClient</see> handle connection state, messages handlers, and connection configuration. There can be many <see cref="NetworkClient">NetworkClient</see> instances in a process at a time, but only one that is connected to a game server (<see cref="NetworkServer">NetworkServer</see>) that uses spawned objects.</para>
    /// <para><see cref="NetworkClient">NetworkClient</see> has an internal update function where it handles events from the transport layer. This includes asynchronous connect events, disconnect events and incoming data from a server.</para>
    /// </summary>
    [AddComponentMenu("Network/NetworkClient")]
    [DisallowMultipleComponent]
    public class NetworkClient : MonoBehaviour, INetworkClient
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkClient));

        TransportV2 socketCreator;
        Peer peer;

        [Tooltip("Authentication component attached to this object")]
        public NetworkAuthenticator authenticator;

        [Header("Events")]
        /// <summary>
        /// Event fires once the Client has connected its Server.
        /// </summary>
        [FormerlySerializedAs("Connected")]
        [SerializeField] NetworkConnectionEvent _connected = new NetworkConnectionEvent();
        public NetworkConnectionEvent Connected => _connected;

        /// <summary>
        /// Event fires after the Client connection has sucessfully been authenticated with its Server.
        /// </summary>
        [FormerlySerializedAs("Authenticated")]
        [SerializeField] NetworkConnectionEvent _authenticated = new NetworkConnectionEvent();
        public NetworkConnectionEvent Authenticated => _authenticated;

        /// <summary>
        /// Event fires after the Client has disconnected from its Server and Cleanup has been called.
        /// </summary>
        [FormerlySerializedAs("Disconnected")]
        [SerializeField] UnityEvent _disconnected = new UnityEvent();
        public UnityEvent Disconnected => _disconnected;

        /// <summary>
        /// The NetworkConnection object this client is using.
        /// </summary>
        public NetworkPlayer Player { get; internal set; }

        internal ConnectState connectState = ConnectState.Disconnected;

        /// <summary>
        /// active is true while a client is connecting/connected
        /// (= while the network is active)
        /// </summary>
        public bool Active => connectState == ConnectState.Connecting || connectState == ConnectState.Connected;

        /// <summary>
        /// This gives the current connection status of the client.
        /// </summary>
        public bool IsConnected => connectState == ConnectState.Connected;

        readonly NetworkTime _time = new NetworkTime();
        /// <summary>
        /// Time kept in this client
        /// </summary>
        public NetworkTime Time
        {
            get { return _time; }
        }

        /// <summary>
        /// NetworkClient can connect to local server in host mode too
        /// </summary>
        public bool IsLocalClient { get; private set; }


        /// <summary>
        /// Connect client to a NetworkServer instance.
        /// </summary>
        /// <param name="serverIp">Address of the server to connect to</param>
        /// <param name="port">The port of the server to connect to</param>
        public void Connect(string serverIp, ushort port)
        {
            if (logger.LogEnabled()) logger.Log("Client address and port:" + serverIp + ":" + port);

            Connect(new IPEndPoint(IPAddress.Parse(serverIp), port));
        }

        /// <summary>
        /// Connect client to a NetworkServer instance.
        /// </summary>
        /// <param name="uri">Address of the server to connect to</param>
        public void Connect(Uri uri)
        {
            Connect(new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port));
        }

        public void Connect(EndPoint endPoint = null)
        {
            if (logger.LogEnabled()) logger.Log("Client Connect: " + endPoint);

            (peer, socketCreator) = PeerUtil.Create(gameObject);

            connectState = ConnectState.Connecting;

            try
            {
                Connection connection = peer.Connect(endPoint ?? socketCreator.GetConnectEndPoint());

                InitializeAuthEvents();

                // setup all the handlers
                Player = GetNewConnection(connection);
                Time.Reset();

                RegisterMessageHandlers();
                Time.UpdateClient(this);
                OnConnected();
            }
            catch (Exception)
            {
                connectState = ConnectState.Disconnected;
                throw;
            }
        }

        internal void ConnectHost(NetworkServer server)
        {
            //todo fix host mode
            throw new NotImplementedException();
            //logger.Log("Client Connect Host to Server");
            //connectState = ConnectState.Connected;

            //InitializeAuthEvents();

            //// create local connection objects and connect them
            //(IConnection c1, IConnection c2) = PipeConnection.CreatePipe();

            //server.SetLocalConnection(this, c2);
            //IsLocalClient = true;
            //Player = GetNewConnection(c1);
            //RegisterHostHandlers();

            //OnConnected().Forget();
        }

        /// <summary>
        /// Creates a new INetworkConnection based on the provided IConnection.
        /// </summary>
        public virtual NetworkPlayer GetNewConnection(Connection connection)
        {
            return new NetworkPlayer(connection);
        }

        void InitializeAuthEvents()
        {
            if (authenticator != null)
            {
                authenticator.OnClientAuthenticated += OnAuthenticated;

                Connected.AddListener(authenticator.OnClientAuthenticateInternal);
            }
            else
            {
                // if no authenticator, consider connection as authenticated
                Connected.AddListener(OnAuthenticated);
            }
        }

        void OnConnected()
        {
            // reset network time stats

            // the handler may want to send messages to the client
            // thus we should set the connected state before calling the handler
            connectState = ConnectState.Connected;
            Connected?.Invoke(Player);

            // todo handle cleanup
            //// start processing messages
            //try
            //{
            //    await Player.ProcessMessagesAsync();
            //}
            //catch (Exception ex)
            //{
            //    logger.LogException(ex);
            //}
            //finally
            //{
            //    Cleanup();

            //    Disconnected?.Invoke();
            //}
        }

        internal void OnAuthenticated(INetworkPlayer conn)
        {
            Authenticated?.Invoke(conn);
        }

        /// <summary>
        /// Disconnect from server.
        /// <para>The disconnect message will be invoked.</para>
        /// </summary>
        public void Disconnect()
        {
            Player?.Disconnect();
        }

        /// <summary>
        /// This sends a network message with a message Id to the server. This message is sent on channel zero, which by default is the reliable channel.
        /// <para>The message must be an instance of a class derived from MessageBase.</para>
        /// <para>The message id passed to Send() is used to identify the handler function to invoke on the server when the message is received.</para>
        /// </summary>
        /// <typeparam name="T">The message type to unregister.</typeparam>
        /// <param name="message"></param>
        /// <param name="channelId"></param>
        /// <returns>True if message was sent.</returns>
        public void Send<T>(T message, int channelId = Channel.Reliable)
        {
            Player.Send(message, channelId);
        }

        public void Send(ArraySegment<byte> segment, int channelId = Channel.Reliable)
        {
            Player.Send(segment, channelId);
        }

        internal void Update()
        {
            // local connection?
            if (!IsLocalClient && Active && connectState == ConnectState.Connected)
            {
                // only update things while connected
                Time.UpdateClient(this);
            }
        }

        internal void RegisterHostHandlers()
        {
            Player.RegisterHandler<NetworkPongMessage>(msg => { });
        }

        internal void RegisterMessageHandlers()
        {
            Player.RegisterHandler<NetworkPongMessage>(Time.OnClientPong);
        }


        /// <summary>
        /// Shut down a client.
        /// <para>This should be done when a client is no longer going to be used.</para>
        /// </summary>
        void Cleanup()
        {
            logger.Log("Shutting down client.");

            IsLocalClient = false;

            connectState = ConnectState.Disconnected;

            if (authenticator != null)
            {
                authenticator.OnClientAuthenticated -= OnAuthenticated;

                Connected.RemoveListener(authenticator.OnClientAuthenticateInternal);
            }
            else
            {
                // if no authenticator, consider connection as authenticated
                Connected.RemoveListener(OnAuthenticated);
            }
        }
    }
}
