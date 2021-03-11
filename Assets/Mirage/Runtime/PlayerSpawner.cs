using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mirage
{

    /// <summary>
    /// Spawns a player as soon as the connection is authenticated
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(PlayerSpawner));

        [FormerlySerializedAs("client")]
        public NetworkClient Client;
        [FormerlySerializedAs("server")]
        public NetworkServer Server;
        [FormerlySerializedAs("sceneManager")]
        public NetworkSceneManager SceneManager;
        [FormerlySerializedAs("clientObjectManager")]
        public ClientObjectManager ClientObjectManager;
        [FormerlySerializedAs("serverObjectManager")]
        public ServerObjectManager ServerObjectManager;
        [FormerlySerializedAs("playerPrefab")]
        public NetworkIdentity PlayerPrefab;

        /// <summary>
        /// Whether to span the player upon connection automatically
        /// </summary>
        public bool AutoSpawn = true;

        // Start is called before the first frame update
        public virtual void Start()
        {
            if (PlayerPrefab == null)
            {
                throw new InvalidOperationException("Assign a player in the PlayerSpawner");
            }
            if (Client != null)
            {
                if (SceneManager != null)
                {
                    SceneManager.ClientSceneChanged.AddListener(OnClientSceneChanged);
                }
                else
                {
                    Client.Authenticated.AddListener(c => Client.Send(new AddPlayerMessage()));
                }

                if (ClientObjectManager != null)
                {
                    ClientObjectManager.RegisterPrefab(PlayerPrefab);
                }
                else
                {
                    throw new InvalidOperationException("Assign a ClientObjectManager");
                }
            }
            if (Server != null)
            {
                Server.Authenticated.AddListener(OnServerAuthenticated);
                if (ServerObjectManager == null)
                {
                    throw new InvalidOperationException("Assign a ServerObjectManager");
                }
            }
        }

        void OnDestroy()
        {
            if (Client != null && SceneManager != null)
            {
                SceneManager.ClientSceneChanged.RemoveListener(OnClientSceneChanged);
                Client.Authenticated.RemoveListener(c => Client.Send(new AddPlayerMessage()));
            }
            if (Server != null)
            {
                Server.Authenticated.RemoveListener(OnServerAuthenticated);
            }
        }

        private void OnServerAuthenticated(INetworkPlayer connection)
        {
            // wait for client to send us an AddPlayerMessage
            connection.RegisterHandler<AddPlayerMessage>(OnServerAddPlayerInternal);
        }

        /// <summary>
        /// Called on the client when a normal scene change happens.
        /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
        /// </summary>
        /// <param name="conn">Connection to the server.</param>
        private void OnClientSceneChanged(string sceneName, SceneOperation sceneOperation)
        {
            if (AutoSpawn && sceneOperation == SceneOperation.Normal)
                RequestServerSpawnPlayer();
        }

        public virtual void RequestServerSpawnPlayer()
        {
            Client.Send(new AddPlayerMessage());
        }

        void OnServerAddPlayerInternal(INetworkPlayer conn, AddPlayerMessage msg)
        {
            logger.Log("PlayerSpawner.OnServerAddPlayer");

            if (conn.Identity != null)
            {
                throw new InvalidOperationException("There is already a player for this connection.");
            }

            OnServerAddPlayer(conn);
        }

        /// <summary>
        /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
        /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public virtual void OnServerAddPlayer(INetworkPlayer conn)
        {
            Transform startPos = GetStartPosition();
            NetworkIdentity player = startPos != null
                ? Instantiate(PlayerPrefab, startPos.position, startPos.rotation)
                : Instantiate(PlayerPrefab);

            ServerObjectManager.AddPlayerForConnection(conn, player.gameObject);
        }

        /// <summary>
        /// This finds a spawn position based on start position objects in the scene.
        /// <para>This is used by the default implementation of OnServerAddPlayer.</para>
        /// </summary>
        /// <returns>Returns the transform to spawn a player at, or null.</returns>
        public virtual Transform GetStartPosition()
        {
            if (startPositions.Count == 0)
                return null;

            if (playerSpawnMethod == PlayerSpawnMethod.Random)
            {
                return startPositions[UnityEngine.Random.Range(0, startPositions.Count)];
            }
            else
            {
                Transform startPosition = startPositions[startPositionIndex];
                startPositionIndex = (startPositionIndex + 1) % startPositions.Count;
                return startPosition;
            }
        }

        public int startPositionIndex;

        /// <summary>
        /// List of transforms where players can be spawned
        /// </summary>
        public List<Transform> startPositions = new List<Transform>();

        /// <summary>
        /// Enumeration of methods of where to spawn player objects in multiplayer games.
        /// </summary>
        public enum PlayerSpawnMethod { Random, RoundRobin }

        /// <summary>
        /// The current method of spawning players used by the PlayerSpawner.
        /// </summary>
        [Tooltip("Round Robin or Random order of Start Position selection")]
        public PlayerSpawnMethod playerSpawnMethod;
    }
}
