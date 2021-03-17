using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirage.InterestManagement
{
    /// <summary>
    /// An interest manager is responsible for showing and hiding objects to players
    /// based on an area if interest policy.
    ///
    /// Implement this class to provide a interest management policy and assign
    /// it to the <see cref="ServerObjectManager"/>
    /// </summary>
    public abstract class InterestManager : MonoBehaviour
    {
        public ServerObjectManager ServerObjectManager;

        public void Start()
        {
            if (ServerObjectManager == null)
                ServerObjectManager = GetComponent<ServerObjectManager>();

            ServerObjectManager.Spawned.AddListener(OnSpawned);

            NetworkServer server = ServerObjectManager.Server;
            if (server == null)
                server = GetComponent<NetworkServer>();

            server.Authenticated.AddListener(OnAuthenticated);
        }

        /// <summary>
        /// Invoked when a player joins the server
        /// It should show all objects relevant to that player
        /// </summary>
        /// <param name="identity"></param>
        protected abstract void OnAuthenticated(INetworkPlayer player);

        /// <summary>
        /// Invoked when an object is spawned in the server
        /// It should show that object to all relevant players
        /// </summary>
        /// <param name="identity">The object just spawned</param>
        protected abstract void OnSpawned(NetworkIdentity identity);

        /// <summary>
        /// Find out all the players that can see an object
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public abstract IReadOnlyCollection<INetworkPlayer> Observers(NetworkIdentity identity);
    }
}
