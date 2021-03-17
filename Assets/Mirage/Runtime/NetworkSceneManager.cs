using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Mirage.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using InvalidEnumArgumentException = System.ComponentModel.InvalidEnumArgumentException;

namespace Mirage
{
    /// <summary>
    /// Provides Scene Management to a NetworkServer and or NetworkClient.
    /// <para>The <see cref="NetworkClient">NetworkClient</see> loads scenes as instructed by the <see cref="NetworkServer">NetworkServer</see>.</para>
    /// <para>The <see cref="NetworkServer">NetworkServer</see> controls the currently active Scene and any additive Load/Unload.</para>
    /// </summary>
    [AddComponentMenu("Network/NetworkSceneManager")]
    [DisallowMultipleComponent]
    public class NetworkSceneManager : MonoBehaviour, INetworkSceneManager
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkSceneManager));

        [FormerlySerializedAs("client")]
        public NetworkClient Client;
        [FormerlySerializedAs("server")]
        public NetworkServer Server;

        /// <summary>
        /// Sets the NetworksSceneManagers GameObject to DontDestroyOnLoad. Default = true.
        /// </summary>
        public bool DontDestroy = true;

        [Header("Events")]

        [FormerlySerializedAs("ClientChangeScene")]
        [SerializeField] ClientSceneChangeEvent _clientChangeScene = new ClientSceneChangeEvent();

        [FormerlySerializedAs("ClientSceneChanged")]
        [SerializeField] ClientSceneChangeEvent _clientSceneChanged = new ClientSceneChangeEvent();

        [FormerlySerializedAs("ServerChangeScene")]
        [SerializeField] ClientSceneChangeEvent _serverChangeScene = new ClientSceneChangeEvent();

        [FormerlySerializedAs("ServerSceneChanged")]
        [SerializeField] ClientSceneChangeEvent _serverSceneChanged = new ClientSceneChangeEvent();

        /// <summary>
        /// Event fires when the Client starts changing scene.
        /// </summary>
        public ClientSceneChangeEvent ClientChangeScene => _clientChangeScene;

        /// <summary>
        /// Event fires after the Client has completed its scene change.
        /// </summary>
        public ClientSceneChangeEvent ClientSceneChanged => _clientSceneChanged;

        /// <summary>
        /// Event fires before Server changes scene.
        /// </summary>
        public ClientSceneChangeEvent ServerChangeScene => _serverChangeScene;

        /// <summary>
        /// Event fires after Server has completed scene change.
        /// </summary>
        public ClientSceneChangeEvent ServerSceneChanged => _serverSceneChanged;

        /// <summary>
        /// The path of the current active scene.
        /// <para>If using additive scenes this will be the first scene.</para>
        /// <para>Value from  <see cref="SceneManager.GetActiveScene()"/> </para>
        /// </summary>
        /// <remarks>
        /// <para>New clients that connect to a server will automatically load this scene.</para>
        /// <para>This is used to make sure that all scene changes are initialized by Mirage.</para>
        /// </remarks>
        public string ActiveScenePath => SceneManager.GetActiveScene().path;

        AsyncOperation clientLoadingOperation;

        /// <summary>
        /// Used by the server to track all additive scenes. To notify clients upon connection 
        /// </summary>
        internal List<string> additiveSceneList = new List<string>();

        /// <summary>
        /// Used by the client to load the full additive scene list that the server has upon connection
        /// </summary>
        internal List<string> pendingAdditiveSceneList = new List<string>();

        IMessageHandler messageHandler;

        public void Start()
        {
            if (DontDestroy)
                DontDestroyOnLoad(gameObject);

            // todo get MessageHandler in a different way, MessageHandler should exist outside of client/server
            if (Client != null)
            {
                Client.Authenticated.AddListener(OnClientAuthenticated);
                Client.Connected.AddListener((_) => { messageHandler = Client.MessageHandler; });
            }
            if (Server != null)
            {
                Server.Authenticated.AddListener(OnServerAuthenticated);
                Server.Started.AddListener(() => { messageHandler = Server.MessageHandler; });
            }
        }

        #region Client

        void RegisterClientMessages()
        {
            messageHandler.RegisterHandler<SceneMessage>(ClientSceneMessage);
            if (!Client.IsLocalClient)
            {
                messageHandler.RegisterHandler<SceneReadyMessage>(ClientSceneReadyMessage);
                messageHandler.RegisterHandler<NotReadyMessage>(ClientNotReadyMessage);
            }
        }

        void OnClientAuthenticated(INetworkPlayer player)
        {
            logger.Log("NetworkSceneManager.OnClientAuthenticated");
            RegisterClientMessages();
        }

        void OnDestroy()
        {
            if (Client != null)
                Client.Authenticated?.RemoveListener(OnClientAuthenticated);
        }

        internal void ClientSceneMessage(INetworkPlayer player, SceneMessage msg)
        {
            if (!Client.IsConnected)
            {
                throw new InvalidOperationException("ClientSceneMessage: cannot change network scene while client is disconnected");
            }
            if (string.IsNullOrEmpty(msg.scenePath))
            {
                throw new ArgumentNullException(nameof(msg.scenePath), $"ClientSceneMessage: {nameof(msg.scenePath)} cannot be empty or null");
            }

            if (logger.LogEnabled()) logger.Log($"ClientSceneMessage: changing scenes from: {ActiveScenePath} to: {msg.scenePath}");

            // Let client prepare for scene change
            OnClientChangeScene(msg.scenePath, msg.sceneOperation);

            //Additive are scenes loaded on server and this client is not a host client
            if (msg.additiveScenes != null && msg.additiveScenes.Length > 0 && Client && !Client.IsLocalClient)
            {
                foreach (string scene in msg.additiveScenes)
                {
                    pendingAdditiveSceneList.Add(scene);
                }
            }

            ApplyOperationAsync(msg.scenePath, msg.sceneOperation).Forget();
        }

        internal void ClientSceneReadyMessage(INetworkPlayer player, SceneReadyMessage msg)
        {
            logger.Log("ClientSceneReadyMessage");

            //Server has finished changing scene. Allow the client to finish.
            if (clientLoadingOperation != null)
                clientLoadingOperation.allowSceneActivation = true;
        }

        internal void ClientNotReadyMessage(INetworkPlayer player, NotReadyMessage msg)
        {
            logger.Log("NetworkSceneManager.OnClientNotReadyMessageInternal");

            Client.Player.IsReady = false;
        }

        /// <summary>
        /// Called from ClientChangeScene immediately before SceneManager.LoadSceneAsync is executed
        /// <para>This allows client to do work / cleanup / prep before the scene changes.</para>
        /// </summary>
        /// <param name="scenePath">Path of the scene that's about to be loaded</param>
        /// <param name="sceneOperation">Scene operation that's about to happen</param>
        internal void OnClientChangeScene(string scenePath, SceneOperation sceneOperation)
        {
            ClientChangeScene?.Invoke(scenePath, sceneOperation);
        }

        /// <summary>
        /// Called on clients when a scene has completed loading, when the scene load was initiated by the server.
        /// <para>Non-Additive Scene changes will cause player objects to be destroyed. The default implementation of OnClientSceneChanged in the NetworkSceneManager is to add a player object for the connection if no player object exists.</para>
        /// </summary>
        /// <param name="scenePath">Path of the scene that was just loaded</param>
        /// <param name="sceneOperation">Scene operation that was just  happen</param>
        internal void OnClientSceneChanged(string scenePath, SceneOperation sceneOperation)
        {
            ClientSceneChanged?.Invoke(scenePath, sceneOperation);

            if (pendingAdditiveSceneList.Count > 0 && Client && !Client.IsLocalClient)
            {
                ApplyOperationAsync(pendingAdditiveSceneList[0], SceneOperation.LoadAdditive).Forget();
                pendingAdditiveSceneList.RemoveAt(0);
                return;
            }

            //set ready after scene change has completed
            if (!Client.Player.IsReady)
                SetClientReady();
        }

        /// <summary>
        /// Signal that the client connection is ready to enter the game.
        /// <para>This could be for example when a client enters an ongoing game and has finished loading the current scene. The server should respond to the message with an appropriate handler which instantiates the players object for example.</para>
        /// </summary>
        public void SetClientReady()
        {
            if (!Client || !Client.Active)
                throw new InvalidOperationException("Ready() called with an null or disconnected client");

            if (logger.LogEnabled()) logger.Log("ClientScene.Ready() called.");

            // Set these before sending the ReadyMessage, otherwise host client
            // will fail in InternalAddPlayer with null readyConnection.
            Client.Player.IsReady = true;

            // Tell server we're ready to have a player object spawned
            Client.Player.Send(new ReadyMessage());
        }

        #endregion

        #region Server

        // called after successful authentication
        void OnServerAuthenticated(INetworkPlayer player)
        {
            logger.Log("NetworkSceneManager.OnServerAuthenticated");

            messageHandler.Send(player, new SceneMessage { scenePath = ActiveScenePath, additiveScenes = additiveSceneList.ToArray() });
            messageHandler.Send(player, new SceneReadyMessage());
        }

        /// <summary>
        /// This causes the server to switch scenes and sets the ActiveScenePath.
        /// <para>Clients that connect to this server will automatically switch to this scene. This automatically sets clients to be not-ready. The clients must call Ready() again to participate in the new scene.</para>
        /// </summary>
        /// <param name="scenePath"></param>
        /// <param name="operation"></param>
        public void ChangeServerScene(string scenePath, SceneOperation sceneOperation = SceneOperation.Normal)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new ArgumentNullException(nameof(scenePath), "ServerChangeScene: " + nameof(scenePath) + " cannot be empty or null");
            }

            if (logger.LogEnabled()) logger.Log("ServerChangeScene " + scenePath);

            // Let server prepare for scene change
            OnServerChangeScene(scenePath, sceneOperation);

            if (!Server.LocalClientActive)
                ApplyOperationAsync(scenePath, sceneOperation).Forget();

            // notify all clients about the new scene
            Server.SendToAll(new SceneMessage { scenePath = scenePath, sceneOperation = sceneOperation });
        }

        /// <summary>
        /// Called from ChangeServerScene immediately before NetworkSceneManager's LoadSceneAsync is executed
        /// <para>This allows server to do work / cleanup / prep before the scene changes.</para>
        /// </summary>
        /// <param name="scenePath">Path of the scene that's about to be loaded</param>
        internal void OnServerChangeScene(string scenePath, SceneOperation operation)
        {
            logger.Log("OnServerChangeScene");

            ServerChangeScene?.Invoke(scenePath, operation);
        }

        /// <summary>
        /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ChangeServerScene().
        /// </summary>
        /// <param name="scenePath">The name of the new scene.</param>
        internal void OnServerSceneChanged(string scenePath, SceneOperation operation)
        {
            logger.Log("OnServerSceneChanged");

            Server.SendToAll(new SceneReadyMessage());

            ServerSceneChanged?.Invoke(scenePath, operation);
        }

        #endregion

        #region Scene Operations

        UniTask ApplyOperationAsync(string scenePath, SceneOperation sceneOperation = SceneOperation.Normal)
        {
            switch (sceneOperation)
            {
                case SceneOperation.Normal: return ApplyNormalOperationAsync(scenePath);
                case SceneOperation.LoadAdditive: return ApplyAdditiveLoadOperationAsync(scenePath);
                case SceneOperation.UnloadAdditive: return ApplyUnloadAdditiveOperationAsync(scenePath);
                default:
                    // Should never happen
                    throw new InvalidEnumArgumentException(nameof(sceneOperation), (int)sceneOperation, typeof(SceneOperation));
            }
        }

        async UniTask ApplyNormalOperationAsync(string scenePath)
        {
            //Scene is already active.
            if (ActiveScenePath.Equals(scenePath))
            {
                FinishLoadScene(scenePath, SceneOperation.Normal);
            }
            else
            {
                clientLoadingOperation = SceneManager.LoadSceneAsync(scenePath);

                //If non host client. Wait for server to finish scene change
                if (Client && Client.Active && !Client.IsLocalClient)
                {
                    clientLoadingOperation.allowSceneActivation = false;
                }

                await clientLoadingOperation;

                logger.Assert(scenePath == ActiveScenePath, "Scene being loaded was not the active scene");
                FinishLoadScene(ActiveScenePath, SceneOperation.Normal);
            }
        }

        async UniTask ApplyAdditiveLoadOperationAsync(string scenePath)
        {
            // Ensure additive scene is not already loaded
            if (SceneManager.GetSceneByPath(scenePath).IsValid())
            {
                logger.LogWarning($"Scene {scenePath} is already loaded");
            }
            else
            {
                await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                additiveSceneList.Add(scenePath);
                FinishLoadScene(scenePath, SceneOperation.LoadAdditive);
            }
        }

        async UniTask ApplyUnloadAdditiveOperationAsync(string scenePath)
        {
            // Ensure additive scene is actually loaded
            if (SceneManager.GetSceneByPath(scenePath).IsValid())
            {
                await SceneManager.UnloadSceneAsync(scenePath, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                additiveSceneList.Remove(scenePath);
                FinishLoadScene(scenePath, SceneOperation.UnloadAdditive);
            }
            else
            {
                logger.LogWarning($"Cannot unload {scenePath} with UnloadAdditive operation");
            }
        }

        internal void FinishLoadScene(string scenePath, SceneOperation sceneOperation)
        {
            // host mode?
            if (Client && Client.IsLocalClient)
            {
                if (logger.LogEnabled()) logger.Log("Host: " + sceneOperation.ToString() + " operation for scene: " + scenePath);

                // call OnServerSceneChanged
                OnServerSceneChanged(scenePath, sceneOperation);

                if (Client.IsConnected)
                {
                    // let client know that we changed scene
                    OnClientSceneChanged(scenePath, sceneOperation);
                }
            }
            // server-only mode?
            else if (Server && Server.Active)
            {
                if (logger.LogEnabled()) logger.Log("Server: " + sceneOperation.ToString() + " operation for scene: " + scenePath);

                OnServerSceneChanged(scenePath, sceneOperation);
            }
            // client-only mode?
            else if (Client && Client.Active && !Client.IsLocalClient)
            {
                if (logger.LogEnabled()) logger.Log("Client: " + sceneOperation.ToString() + " operation for scene: " + scenePath);

                OnClientSceneChanged(scenePath, sceneOperation);
            }
        }

        #endregion
    }
}
